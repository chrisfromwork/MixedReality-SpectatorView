#include "..\SharedFiles\pch.h"
#include "ChessboardStereoCalibration.h"

struct Transformation
{
	cv::Matx33d R;
	cv::Vec3d t;

	// Construct an identity transformation.
	Transformation() : R(cv::Matx33d::eye()), t(0., 0., 0.) {}

	// Construct from H
	Transformation(const cv::Matx44d &H) : R(H.get_minor<3, 3>(0, 0)), t(H(0, 3), H(1, 3), H(2, 3)) {}

	// Create homogeneous matrix from this transformation
	cv::Matx44d to_homogeneous() const
	{
		return cv::Matx44d(
			// row 1
			R(0, 0),
			R(0, 1),
			R(0, 2),
			t(0),
			// row 2
			R(1, 0),
			R(1, 1),
			R(1, 2),
			t(1),
			// row 3
			R(2, 0),
			R(2, 1),
			R(2, 2),
			t(2),
			// row 4
			0,
			0,
			0,
			1);
	}

	// Construct a transformation equivalent to this transformation followed by the second transformation
	Transformation compose_with(const Transformation &second_transformation) const
	{
		// get this transform
		cv::Matx44d H_1 = to_homogeneous();
		// get the transform to be composed with this one
		cv::Matx44d H_2 = second_transformation.to_homogeneous();
		// get the combined transform
		cv::Matx44d H_3 = H_1 * H_2;
		return Transformation(H_3);
	}
};

static cv::Mat color_to_opencv(const uchar *im, const int &width, const int &height)
{
	cv::Mat cv_image_with_alpha(height, width, CV_8UC4, (void *)im);
	cv::Mat cv_image_no_alpha;
	cv::cvtColor(cv_image_with_alpha, cv_image_no_alpha, cv::COLOR_BGRA2BGR);
	return cv_image_no_alpha;
}

bool find_chessboard_corners_helper(const cv::Mat &main_color_image,
	const cv::Mat &secondary_color_image,
	const cv::Size &chessboard_pattern,
	std::vector<cv::Point2f> &main_chessboard_corners,
	std::vector<cv::Point2f> &secondary_chessboard_corners)
{
	bool found_chessboard_main = cv::findChessboardCorners(main_color_image,
		chessboard_pattern,
		main_chessboard_corners);
	bool found_chessboard_secondary = cv::findChessboardCorners(secondary_color_image,
		chessboard_pattern,
		secondary_chessboard_corners);

	// Cover the failure cases where chessboards were not found in one or both images.
	if (!found_chessboard_main || !found_chessboard_secondary)
	{
		return false;
	}
	// Before we go on, there's a quick problem with calibration to address.  Because the chessboard looks the same when
	// rotated 180 degrees, it is possible that the chessboard corner finder may find the correct points, but in the
	// wrong order.

	// A visual:
	//        Image 1                  Image 2
	// .....................    .....................
	// .....................    .....................
	// .........xxxxx2......    .....xxxxx1..........
	// .........xxxxxx......    .....xxxxxx..........
	// .........xxxxxx......    .....xxxxxx..........
	// .........1xxxxx......    .....2xxxxx..........
	// .....................    .....................
	// .....................    .....................

	// The problem occurs when this case happens: the find_chessboard() function correctly identifies the points on the
	// chessboard (shown as 'x's) but the order of those points differs between images taken by the two cameras.
	// Specifically, the first point in the list of points found for the first image (1) is the *last* point in the list
	// of points found for the second image (2), though they correspond to the same physical point on the chessboard.

	// To avoid this problem, we can make the assumption that both of the cameras will be oriented in a similar manner
	// (e.g. turning one of the cameras upside down will break this assumption) and enforce that the vector between the
	// first and last points found in pixel space (which will be at opposite ends of the chessboard) are pointing the
	// same direction- so, the dot product of the two vectors is positive.

	cv::Vec2f main_image_corners_vec = main_chessboard_corners.back() - main_chessboard_corners.front();
	cv::Vec2f secondary_image_corners_vec = secondary_chessboard_corners.back() - secondary_chessboard_corners.front();
	if (main_image_corners_vec.dot(secondary_image_corners_vec) <= 0.0)
	{
		std::reverse(secondary_chessboard_corners.begin(), secondary_chessboard_corners.end());
	}
	return true;
}

Transformation stereo_calibration(
	const cv::Matx33f &main_camera_matrix,
	const cv::Matx33f &secondary_camera_matrix,
	std::vector<float> main_dist_coeff,
	std::vector<float> secondary_dist_coeff,
	const std::vector<std::vector<cv::Point2f>> &main_chessboard_corners_list,
	const std::vector<std::vector<cv::Point2f>> &secondary_chessboard_corners_list,
	const cv::Size &image_size,
	const cv::Size &chessboard_pattern,
	float chessboard_square_length)
{
	// We have points in each image that correspond to the corners that the findChessboardCorners function found.
	// However, we still need the points in 3 dimensions that these points correspond to. Because we are ultimately only
	// interested in find a transformation between two cameras, these points don't have to correspond to an external
	// "origin" point. The only important thing is that the relative distances between points are accurate. As a result,
	// we can simply make the first corresponding point (0, 0) and construct the remaining points based on that one. The
	// order of points inserted into the vector here matches the ordering of findChessboardCorners. The units of these
	// points are in millimeters, mostly because the depth provided by the depth cameras is also provided in
	// millimeters, which makes for easy comparison.
	std::vector<cv::Point3f> chessboard_corners_world;
	for (int h = 0; h < chessboard_pattern.height; ++h)
	{
		for (int w = 0; w < chessboard_pattern.width; ++w)
		{
			chessboard_corners_world.emplace_back(
				cv::Point3f{ w * chessboard_square_length, h * chessboard_square_length, 0.0 });
		}
	}

	// Calibrating the cameras requires a lot of data. OpenCV's stereoCalibrate function requires:
	// - a list of points in real 3d space that will be used to calibrate*
	// - a corresponding list of pixel coordinates as seen by the first camera*
	// - a corresponding list of pixel coordinates as seen by the second camera*
	// - the camera matrix of the first camera
	// - the distortion coefficients of the first camera
	// - the camera matrix of the second camera
	// - the distortion coefficients of the second camera
	// - the size (in pixels) of the images
	// - R: stereoCalibrate stores the rotation matrix from the first camera to the second here
	// - t: stereoCalibrate stores the translation vector from the first camera to the second here
	// - E: stereoCalibrate stores the essential matrix here (we don't use this)
	// - F: stereoCalibrate stores the fundamental matrix here (we don't use this)
	//
	// * note: OpenCV's stereoCalibrate actually requires as input an array of arrays of points for these arguments,
	// allowing a caller to provide multiple frames from the same camera with corresponding points. For example, if
	// extremely high precision was required, many images could be taken with each camera, and findChessboardCorners
	// applied to each of those images, and OpenCV can jointly solve for all of the pairs of corresponding images.
	// However, to keep things simple, we use only one image from each device to calibrate.  This is also why each of
	// the vectors of corners is placed into another vector.
	//
	// A function in OpenCV's calibration function also requires that these points be F32 types, so we use those.
	// However, OpenCV still provides doubles as output, strangely enough.
	std::vector<std::vector<cv::Point3f>> chessboard_corners_world_nested_for_cv(main_chessboard_corners_list.size(),
		chessboard_corners_world);

	// Finally, we'll actually calibrate the cameras.
	// Pass secondary first, then main, because we want a transform from secondary to main.
	Transformation tr;
	double error = cv::stereoCalibrate(chessboard_corners_world_nested_for_cv,
		secondary_chessboard_corners_list,
		main_chessboard_corners_list,
		secondary_camera_matrix,
		secondary_dist_coeff,
		main_camera_matrix,
		main_dist_coeff,
		image_size,
		tr.R, // output
		tr.t, // output
		cv::noArray(),
		cv::noArray(),
		cv::CALIB_FIX_INTRINSIC);
	return tr;
}

ChessboardStereoCalibration::ChessboardStereoCalibration()
{

}

void ChessboardStereoCalibration::Initialize()
{
	mainCameraMatCorners.clear();
	cameraMatCorners.clear();
}

bool ChessboardStereoCalibration::TryCalibrate(
	int numImages,
	int requiredImages,
	unsigned char *images,
	int width,
	int height,
	int pixelSize,
	int chessboardWidth,
	int chessboardHeight,
	float chessboardSideLength,
	float *cameraProperties,
	float *cameraDistCoeffProperties,
	bool *completed,
	float *cameraTransforms)
{
	cv::Size chessboard_pattern(chessboardWidth, chessboardHeight);
	float chessboard_square_length = chessboardSideLength;

	std::vector<cv::Matx33f> cameraMatrices;
	std::vector<std::vector<float>> cameraDistCoeffs;
	for (int i = 0; i < numImages; i++)
	{
		cv::Matx33f cameraMat = cv::Matx33f::eye();
		cameraMat(0, 0) = cameraProperties[i * 4]; // i.fx;
		cameraMat(1, 1) = cameraProperties[i * 4 + 1]; // i.fy;
		cameraMat(0, 2) = cameraProperties[i * 4 + 2]; // i.cx;
		cameraMat(1, 2) = cameraProperties[i * 4 + 3]; // i.cy;
		cameraMatrices.push_back(cameraMat);

		std::vector<float> cameraDistCoeff{ // i.k1, i.k2, i.p1, i.p2, i.k3, i.k4, i.k5, i.k6 
			cameraDistCoeffProperties[i * 8],
			cameraDistCoeffProperties[i * 8 + 1],
			cameraDistCoeffProperties[i * 8 + 2],
			cameraDistCoeffProperties[i * 8 + 3],
			cameraDistCoeffProperties[i * 8 + 4],
			cameraDistCoeffProperties[i * 8 + 5],
			cameraDistCoeffProperties[i * 8 + 6],
			cameraDistCoeffProperties[i * 8 + 7] };
		cameraDistCoeffs.push_back(cameraDistCoeff);
	}

	int imageSize = width * height * pixelSize;
	cv::Mat mainCameraMat = color_to_opencv(images, width, height);
	std::vector<cv::Mat> cameraMats;
	bool calibratedAllImages = true;
	bool foundAllChessboards = false;
	std::vector<Transformation> transformations;
	for (int i = 1; i < numImages; i++)
	{
		cv::Mat tempMat = color_to_opencv(&(images[i * imageSize]), width, height);
		std::vector<cv::Point2f> main_chessboard_corners;
		std::vector<cv::Point2f> secondary_chessboard_corners;
		bool foundChessboards = find_chessboard_corners_helper(
			mainCameraMat,
			tempMat,
			chessboard_pattern,
			main_chessboard_corners,
			secondary_chessboard_corners);

		if (foundChessboards)
		{
			if (cameraMatCorners.size() == (i - 1))
			{
				mainCameraMatCorners.push_back({ main_chessboard_corners });
				cameraMatCorners.push_back({ secondary_chessboard_corners });
			}
			else
			{
				mainCameraMatCorners.at(i - 1).push_back(main_chessboard_corners);
				cameraMatCorners.at(i - 1).push_back(secondary_chessboard_corners);
			}
		}
		else
		{
			foundAllChessboards = false;
		}

		if (cameraMatCorners.size() > (i - 1) &&
			cameraMatCorners.at(i - 1).size() > requiredImages &&
			mainCameraMatCorners.at(i - 1).size() == cameraMatCorners.at(i - 1).size())
		{
			Transformation transformation = stereo_calibration(
				cameraMatrices.at(0),
				cameraMatrices.at(i),
				cameraDistCoeffs.at(0),
				cameraDistCoeffs.at(i),
				mainCameraMatCorners.at(i - 1),
				cameraMatCorners.at(i - 1),
				cv::Size(width, height),
				chessboard_pattern,
				chessboard_square_length);
			transformations.push_back(transformation);
		}
		else
		{
			transformations.push_back(Transformation{});
			calibratedAllImages = false;
		}
	}

	if (calibratedAllImages)
	{
		*completed = true;
		int matrixSize = 16;
		for (int i = 0; i < transformations.size(); i++)
		{
			auto transformation = transformations.at(i);
			cv::Mat rvec;
			cv::Rodrigues(transformation.R, rvec);
			cameraTransforms[i * matrixSize] = transformation.t.col(0).row(0).val[0];
			cameraTransforms[i * matrixSize + 1] = transformation.t.col(0).row(1).val[0];
			cameraTransforms[i * matrixSize + 2] = transformation.t.col(0).row(2).val[0];
			cameraTransforms[i * matrixSize + 3] = rvec.at<double>(0, 0);
			cameraTransforms[i * matrixSize + 4] = rvec.at<double>(0, 1);
			cameraTransforms[i * matrixSize + 5] = rvec.at<double>(0, 2);
			cameraTransforms[i * matrixSize + 4] = transformation.R.col(0).row(1).val[0];
			cameraTransforms[i * matrixSize + 5] = transformation.R.col(0).row(2).val[0];
			cameraTransforms[i * matrixSize + 6] = transformation.R.col(1).row(0).val[0];
			cameraTransforms[i * matrixSize + 7] = transformation.R.col(1).row(1).val[0];
			cameraTransforms[i * matrixSize + 8] = transformation.R.col(1).row(2).val[0];
			cameraTransforms[i * matrixSize + 9] = transformation.R.col(2).row(0).val[0];
			cameraTransforms[i * matrixSize + 10] = transformation.R.col(2).row(1).val[0];
			cameraTransforms[i * matrixSize + 11] = transformation.R.col(2).row(2).val[0];
			cameraTransforms[i * matrixSize + 12] = 0;
			cameraTransforms[i * matrixSize + 13] = 0;
			cameraTransforms[i * matrixSize + 14] = 0;
			cameraTransforms[i * matrixSize + 15] = 0;
		}

		mainCameraMatCorners.clear();
		cameraMatCorners.clear();
	}
	else
	{
		*completed = false;
	}

	return foundAllChessboards;
}