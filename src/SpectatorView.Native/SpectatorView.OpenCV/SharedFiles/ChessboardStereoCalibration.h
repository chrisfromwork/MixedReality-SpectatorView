#pragma once
#include "pch.h"

class ChessboardStereoCalibration
{
public:
	ChessboardStereoCalibration();
	void Initialize();
	bool TryCalibrate(
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
		float *cameraTransforms);

private:
	std::vector < std::vector<std::vector<cv::Point2f>>> mainCameraMatCorners;
	std::vector<std::vector<std::vector<cv::Point2f>>> cameraMatCorners;
	std::vector<std::vector<std::vector<cv::Point2f>>> cameraMatCornersPnP;
};