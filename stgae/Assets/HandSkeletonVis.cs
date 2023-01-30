#region Headers
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

using System.Net;
using System.Net.Sockets;
using System.Threading;
#endregion

public class HandSkeletonVis : MonoBehaviour
{
    #region Global variables
    public GameObject jointObject;

    public int jointsNum = 26;
    public Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);
    public Color[] jointsColor = new Color[26] {
        Color.yellow, Color.clear, Color.magenta, Color.magenta, Color.magenta,
        Color.magenta, Color.cyan, Color.cyan, Color.cyan, Color.cyan,
        Color.cyan, Color.red, Color.red, Color.red, Color.red,
        Color.red, Color.green, Color.green, Color.green, Color.green,
        Color.green, Color.blue, Color.blue, Color.blue, Color.blue, Color.blue
    };

    public GameObject line;
    public float lineWidth = 0.005f;
    public int fingersNum = 5;
    public int[,] fingers = new int[5, 6] {
        {0, 2, 3, 4, 5, 5},
        {0, 6, 7, 8, 9, 10},
        {0, 11, 12, 13, 14, 15},
        {0, 16, 17, 18, 19, 20},
        {0, 21, 22, 23, 24, 25}
    };
    public Color[] fingersColor = new Color[5] {
        Color.magenta, Color.cyan, Color.red, Color.green, Color.blue
    };

    public float minDist = 0.02f;
    public float minDistRecieve = 0.15f;
    public float tmpDistL = 1f;
    public float tmpDistR = 1f;
    public float tmpDistLRecieve = 1f;
    public float tmpDistRRecieve = 1f;

    Vector3[] jointsL = new Vector3[26];
    Vector3[] jointsR = new Vector3[26];
    Vector3[] jointsSend = new Vector3[26];
    Vector3[] jointsRecieve = new Vector3[26];

    List<GameObject> jointObjectsL = new List<GameObject>();
    List<GameObject> jointObjectsR = new List<GameObject>();

    List<LineRenderer> fingerBonesL = new List<LineRenderer>();
    List<LineRenderer> fingerBonesR = new List<LineRenderer>();

    // two dynamic line render container, one for the orignal data, the other for the recieving data
    List<LineRenderer> lrWords = new List<LineRenderer>();
    List<LineRenderer> lrWordsRecieve = new List<LineRenderer>();

    // Log the line event: 0 is not in the line, 1 is in line, 2 is the starting point of the line
    //                     3 is the end of the line, 4 is cleaning the forward line
    List<int> lineEvent = new List<int>();
    List<Vector3> pointGroundTruth = new List<Vector3>();
    List<Vector3> point = new List<Vector3>();
    int lastVisitedPoint = 17;

    MixedRealityPose pose;

    public bool isHandInScene = false;
    public bool leftHandInScene = false;
    public bool rightHandInScene = false;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        Debug.Log("Initializing the custom hand visualization...");
#endif

        BoneJointInit();
    }

    // Update is called once per frame
    void Update()
    {
        BoneJointRender();
    }

    void BoneJointInit()
    {
        jointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jointObject.name = "Joint";
        // Initialization: joints
        for (int i = 0; i < jointsNum; i++)
        {
            GameObject obj1 = Instantiate(jointObject, this.transform);
            jointObjectsL.Add(obj1);

            GameObject obj2 = Instantiate(jointObject, this.transform);
            jointObjectsR.Add(obj2);
        }

        //line = new GameObject();
        //line.name = "Bone";
        //line.AddComponent<LineRenderer>();
        // Initialization: bones
        for (int i = 0; i < fingersNum; i++)
        {
            LineRenderer lr1 = InitialLine(fingersColor[i], fingersColor[i], lineWidth, lineWidth, 6);
            fingerBonesL.Add(lr1);

            LineRenderer lr2 = InitialLine(fingersColor[i], fingersColor[i], lineWidth, lineWidth, 6);
            fingerBonesR.Add(lr2);
        }
    }

    LineRenderer InitialLine(Color startColor, Color endColor, float startWidth, float endWidth, int positionCount)
    {
        GameObject obj = Instantiate(line, this.transform);
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.positionCount = positionCount;
        lr.startColor = startColor;
        lr.endColor = endColor;
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;

        Debug.Log(startColor);

        return lr;
    }

    void BoneJointRender()
    {
        // Since we want to only render tracked hands
        //      First, we need to disable all joints and bones variables.
        //      Then, if the hand is tracked, the status will be set to true.
        for (int i = 0; i < jointsNum; i++)
        {
            jointObjectsL[i].GetComponent<Renderer>().enabled = false;
            jointObjectsR[i].GetComponent<Renderer>().enabled = false;
        }

        for (int i = 0; i < fingersNum; i++)
        {
            fingerBonesL[i].enabled = false;
            fingerBonesR[i].enabled = false;
        }

        // Track all the joints of left and right hands respectively
        //      First, initilize buffers to log hand positions before tracking.
        //      Then, track the hand joints.

        jointsL = new Vector3[26];
        jointsR = new Vector3[26];

        for (int i = 0; i < jointsNum; i++)
        {
            if (HandJointUtils.TryGetJointPose((TrackedHandJoint)(i + 1), Handedness.Left, out pose))
            {
                jointsL[i] = pose.Position;
                jointObjectsL[i].GetComponent<Renderer>().enabled = true;

                //Debug.Log("Left hand, joint - " + (i + 1) + " "
                //    + (TrackedHandJoint)(i + 1)
                //    + ": " + jointsL[i]);
            }

            if (HandJointUtils.TryGetJointPose((TrackedHandJoint)(i + 1), Handedness.Right, out pose))
            {
                jointsR[i] = pose.Position;
                jointObjectsR[i].GetComponent<Renderer>().enabled = true;

                //Debug.Log("Right hand, joint - " + (i + 1) + " "
                //    + (TrackedHandJoint)(i + 1)
                //    + ": " + jointsR[i]);
            }
        }

        // Set joints position
        for (int i = 0; i < jointsNum; i++)
        {
            jointObjectsL[i].transform.position = jointsL[i];
            jointObjectsL[i].GetComponent<Renderer>().material.color = jointsColor[i];
            jointObjectsL[i].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            jointObjectsR[i].transform.position = jointsR[i];
            jointObjectsR[i].GetComponent<Renderer>().material.color = jointsColor[i];
            jointObjectsR[i].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }

        // Draw bone lines
        for (int i = 0; i < fingersNum; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                if (jointObjectsL[0].GetComponent<Renderer>().enabled)
                {
                    fingerBonesL[i].enabled = true;
                    fingerBonesL[i].SetPosition(j, jointsL[fingers[i, j]]);
                    fingerBonesL[i].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                }

                if (jointObjectsR[0].GetComponent<Renderer>().enabled)
                {
                    fingerBonesR[i].enabled = true;
                    fingerBonesR[i].SetPosition(j, jointsR[fingers[i, j]]);
                    fingerBonesR[i].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                }
            }
        }

        // Judge whelter any hand is in scene 
        if (!jointObjectsL[5].GetComponent<Renderer>().enabled && !jointObjectsR[5].GetComponent<Renderer>().enabled)
        {
            isHandInScene = false;
            leftHandInScene = false;
            rightHandInScene = false;
        }
        else
        {
            isHandInScene = true;
            leftHandInScene = jointObjectsL[5].GetComponent<Renderer>().enabled ? true : false;
            rightHandInScene = jointObjectsR[5].GetComponent<Renderer>().enabled ? true : false;
        }
    }
}
