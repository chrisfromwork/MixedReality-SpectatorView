using Microsoft.Azure.Kinect.Sensor;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    public class CustomCameraTransform
    {
        private const string RootDirectoryName = "CameraTransforms";

        public Matrix4x4 Transform;
        public Matrix4x4 ExtrinsicsTransform;

        public CustomCameraTransform(Matrix4x4 transform, Matrix4x4 extrinsicsTransform)
        {
            this.Transform = transform;
            this.ExtrinsicsTransform = extrinsicsTransform;
        }

        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        public static bool TryDeserialize(byte[] payload, out CustomCameraTransform cameraTransform)
        {
            cameraTransform = null;

            try
            {
                var str = Encoding.UTF8.GetString(payload);
                cameraTransform = JsonUtility.FromJson<CustomCameraTransform>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception thrown deserializing a custom camera transform: {e.ToString()}");
                return false;
            }
        }

#if UNITY_EDITOR
        public static CustomCameraTransform LoadCameraIntrinsics(string path)
        {
            if (File.Exists(path))
            {
                var fileData = File.ReadAllBytes(path);
                if (CustomCameraTransform.TryDeserialize(fileData, out var cameraTransform))
                {
                    return cameraTransform;
                }
            }

            Debug.LogError($"Failed to load custom camera transform file {path}");
            return null;
        }

        public static string SaveCameraTransform(CustomCameraTransform cameraTransform)
        {
            var directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RootDirectoryName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string path = Path.Combine(directoryPath, $"CameraTransform.json");
            int i = 0;
            while (File.Exists(path))
            {
                path = Path.Combine(directoryPath, $"CameraTransform_{i}.json");
                i++;
            }

            var payload = cameraTransform.Serialize();
            File.WriteAllBytes(path, payload);
            return path;
        }

        public static CustomCameraTransform LoadCameraTransform(string path)
        {
            if (File.Exists(path))
            {
                var fileData = File.ReadAllBytes(path);
                if (CustomCameraTransform.TryDeserialize(fileData, out var cameraTransform))
                {
                    return cameraTransform;
                }
            }

            Debug.LogError($"Failed to load camera transform file {path}");
            return null;
        }
#endif
    }
}
