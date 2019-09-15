using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Windows.Kinect;

public class KinectData : MonoBehaviour
{
    KinectSensor sensor;
    CoordinateMapper coordinateMapper;
    MultiSourceFrameReader reader;

    public AudioManager audioManager;
    public CameraSpacePoint[] cameraSpacePoints;
    public int depthWidth, depthHeight;

    ushort[] depthData;
    // public byte[] depthColorData;
    public byte[] depthZoneData;

    void Start()
    {
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            coordinateMapper = sensor.CoordinateMapper;
            FrameDescription depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
            depthWidth = depthFrameDesc.Width;
            depthHeight = depthFrameDesc.Height;
            cameraSpacePoints = new CameraSpacePoint[depthFrameDesc.LengthInPixels];

            depthData = new ushort[depthFrameDesc.LengthInPixels];

            if (!sensor.IsOpen)
            {
                sensor.Open();
            }
        }
        else
        {
            Debug.LogError("Can't find Kinect Sensor.");
        }
        StartCoroutine("ObjectDetection");
    }

    // Update is called once per frame
    void Update()
    {
        if (reader != null)
        {

            MultiSourceFrame MSFrame = reader.AcquireLatestFrame();

            if (MSFrame != null)
            {
                using (DepthFrame frame = MSFrame.DepthFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        frame.CopyFrameDataToArray(depthData);
                        coordinateMapper.MapDepthFrameToCameraSpace(depthData, cameraSpacePoints);
                        // Debug.Log(cameraSpacePoints.Length);
                    }
                }

                MSFrame = null;
            }
        }
    }

    public byte[] ImageToByteArray(System.Drawing.Image imageIn)
    {
        using (var ms = new MemoryStream())
        {
            imageIn.Save(ms, imageIn.RawFormat);
            return ms.ToArray();
        }
    }

    IEnumerator ObjectDetection()
    {
        while (true)
        {
            if (reader != null)
            {
                MultiSourceFrame MSFrame = reader.AcquireLatestFrame();

                if (MSFrame != null)
                {
                    using (ColorFrame fr = MSFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (fr != null)
                        {
                            var frameMetadata = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
                            byte[] color = new byte[frameMetadata.BytesPerPixel * frameMetadata.LengthInPixels];
                            fr.CopyConvertedFrameDataToArray(color, ColorImageFormat.Bgra);
                            var bitmap = new System.Drawing.Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                            var bitmapData = bitmap.LockBits(
                                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                bitmap.PixelFormat);
                            System.Runtime.InteropServices.Marshal.Copy(color, 0, bitmapData.Scan0, color.Length);
                            bitmap.UnlockBits(bitmapData);
                            System.Drawing.Bitmap resized = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(bitmap.Width / 2, bitmap.Height / 2));
                            try
                            {
                                resized.Save("test.jpeg");
                                System.Drawing.Image currFrame = System.Drawing.Image.FromFile("test.jpeg");
                                byte[] currFrameByte = ImageToByteArray(currFrame);

                                StartCoroutine(MakeRequest(currFrameByte));
                            }
                            catch (Exception e)
                            {

                            }

                        }
                    }
                    MSFrame = null;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MakeRequest(byte[] byteData)
    {
        var client = new HttpClient();
        var uri = "https://soundsense.cognitiveservices.azure.com/vision/v2.0/detect";

        using (var content = new ByteArrayContent(byteData))
        {
            UnityWebRequest req = UnityWebRequest.Post(uri, "");
            req.uploadHandler = new UploadHandlerRaw(byteData);
            req.SetRequestHeader("Content-Type", "application/octet-stream");
            req.SetRequestHeader("Ocp-Apim-Subscription-Key", "11c6f95c55b349779fa1dc5ffcab0a36");
            yield return req.SendWebRequest();
            Debug.Log(req.downloadHandler.text);
            // VisionData data = JsonUtility.FromJson<VisionData>(req.downloadHandler.text);
            // var vals = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var vals = JsonConvert.DeserializeObject<VisionData>(req.downloadHandler.text);
            GetHighestConfidence(vals);
        }
    }

    private void GetHighestConfidence(VisionData data)
    {
        float maxConfidence = 0;
        VisionObject obj = null;

        if (data != null)
        {
            foreach (var vobj in data.objects)
            {
                if (vobj.confidence > maxConfidence)
                {
                    maxConfidence = vobj.confidence;
                    obj = vobj;
                }
            }
            if (obj != null)
            {
                int midX = (obj.rectangle.x + obj.rectangle.w) / 2;
                int midY = (obj.rectangle.y + obj.rectangle.h) / 2;
                String objString = obj.obj;
                StartCoroutine(audioManager.playWord(midX, midY, objString));
            }
        }
    }


}

