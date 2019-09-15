using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Windows.Kinect;

public class AudioManager : MonoBehaviour
{

    public KinectData data;
    public int resolution;
    public int convolutions;

    public GameObject speaker;

    public GameObject closestObject;
    public GameObject speakingObject;

    private List<GameObject> speakerList;

    void Start()
    {
        speakerList = new List<GameObject>();
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                speakerList.Add(Instantiate(speaker, new Vector3(0, 0, 0), Quaternion.identity));
                UnityEngine.AudioSource src = speakerList[j + resolution * i].GetComponent<UnityEngine.AudioSource>();
                src.pitch = (j + resolution * i) * ((float)2 / (resolution * resolution)) + 1;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        CameraSpacePoint[] point = data.cameraSpacePoints;
        // 512*424 = x*y i = x+y*512
        if (point != null && point.Length > 200000)
        {
            Vector3[,] depthMap = new Vector3[512, 424];
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 424; y++)
                {
                    CameraSpacePoint pt = point[x + y * 512];
                    depthMap[x, y] = new Vector3((int)(pt.X * 1000), (int)(pt.Y * 1000), (int)(pt.Z * 1000));
                }
            }

            Vector3[] audioLocations = CameraPointsToAudioLocations(Convolution(depthMap, convolutions), resolution);
            Vector3 smallestTransform = new Vector3(10000, 10000, 10000);

            for (int i = 0; i < audioLocations.Length; i++)
            {
                if (speakerList.Count != audioLocations.Length)
                    break;

                float x = audioLocations[i].x / 1000;
                float y = audioLocations[i].y / 1000;
                float z = audioLocations[i].z / 1000;
                float d = (float)Math.Sqrt(x * x + z * z);

                float[] r = DoubleAngle(x, z);
                float[] rn = DoubleAngle(r[0], r[1]);

                if (audioLocations[i].z > 500 && audioLocations[i].z < 6000)
                    speakerList[i].gameObject.transform.position = new Vector3(-rn[0], y, rn[1]);
                else
                    speakerList[i].gameObject.transform.position = new Vector3(-2 * x * 8, y, (10 - x * x));

                // Debug.Log(audioLocations[i]);

                if (audioLocations[i].magnitude != 0 && audioLocations[i].magnitude < smallestTransform.magnitude)
                {
                    smallestTransform = audioLocations[i];
                }
            }

            closestObject.transform.position =
                new Vector3(-DoubleAngle(smallestTransform.x / 1000, smallestTransform.z / 1000)[0], smallestTransform.y / 1000, DoubleAngle(smallestTransform.x / 1000, smallestTransform.z / 1000)[1]);
        }
        else
        {
            Debug.Log("No data");
        }

    }

    float[] DoubleAngle(float x, float z)
    {
        float d = (float)Math.Sqrt(x * x + z * z);
        float[] res = new float[2];
        res[0] = 2 * x * z / d;
        res[1] = (z * z - x * x) / d;
        return res;
    }

    public Vector3[,] Convolution(Vector3[,] depthMap, int convSize)
    {
        for (int i = 0; i < depthMap.GetLength(0); i += convSize)
        {
            for (int j = 0; j < depthMap.GetLength(1); j += convSize)
            {
                Vector3 sum = new Vector3(0, 0, 0);
                for (int a = 0; a < convSize; a++)
                {
                    for (int b = 0; b < convSize; b++)
                    {
                        if (a + i < depthMap.GetLength(0) && b + j < depthMap.GetLength(1))
                            sum += depthMap[a + i, b + j];
                    }
                }
                for (int a = 0; a < convSize; a++)
                {
                    for (int b = 0; b < convSize; b++)
                    {
                        if (a + i < depthMap.GetLength(0) && b + j < depthMap.GetLength(1))
                            depthMap[a + i, b + j] = sum / (convSize * convSize);
                    }
                }
            }
        }

        return depthMap;
    }

    public Vector3[] CameraPointsToAudioLocations(Vector3[,] depthMap, int resolution)
    {
        int yOSize = depthMap.GetLength(0);
        int xOSize = depthMap.GetLength(1);

        int ySize = (int)Math.Ceiling((double)yOSize / resolution);
        int xSize = (int)Math.Ceiling((double)xOSize / resolution);

        int[] maxes = new int[resolution * resolution];
        Vector3[] maxIndicies = new Vector3[maxes.Length];
        for (int i = 0; i < maxes.Length; i++)
        {
            maxes[i] = 100000;
        }
        for (int i = 0; i < yOSize; i++)
        {
            for (int j = 0; j < xOSize; j++)
            {
                int tempCell = i / ySize * resolution + j / xSize;
                Vector3 pt = depthMap[i, j];
                int dist = (int)Math.Sqrt(pt.x * pt.x + pt.y * pt.y + pt.z * pt.z);
                // Debug.Log(pt);
                if (dist >= 0 && dist < maxes[tempCell])
                {
                    maxes[tempCell] = dist;
                    maxIndicies[tempCell] = new Vector3(pt.x, pt.y, pt.z);
                }
            }
        }

        return maxIndicies;
    }

    public IEnumerator playWord(int midX, int midY, String objString)
    {
        Debug.Log($"Trying to play {objString}");
        // var asset = Resources.Load<AudioClip>(objString + ".m4a");
        string path = "D:\\Installations\\UnityKinectDepthExplorer-master\\Assets\\Resources\\"+objString+".wav";
        string url = "file:///" + path;
        Debug.Log(url);
        using (var www = new WWW(url))
        {
            yield return www;
            speakingObject.GetComponent<UnityEngine.AudioSource>().clip = www.GetAudioClip();
        }
        float speakerX = (float)(2 * (midX - 480.0) / 480);
        float speakerY = (float)(1 * (midY - 270.0) / 270);
        float speakerZ = (float)Math.Sqrt(4 - (speakerX * speakerX + speakerY * speakerY));
        speakingObject.transform.position = new Vector3(speakerX, speakerY, speakerZ);
        if (!speakingObject.GetComponent<UnityEngine.AudioSource>().isPlaying) {
            speakingObject.GetComponent<UnityEngine.AudioSource>().Play();
        }
    }
}
