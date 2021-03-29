using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

public class HandInput : MonoBehaviour
{
    [SerializeField] OVRHand ovrHand;
    [SerializeField] OVRSkeleton ovrSkeleton;
    [SerializeField] Transform indexTipMarker;
    [SerializeField] Transform lastPinchMarker;
    [SerializeField] float forceMultiplier = 1000.0f;

    HashSet<InteractionTriggerDetector> currentlyIntersectingInteractionTriggers = new HashSet<InteractionTriggerDetector>();
    ArticulationBody currentArticulationBody;
    float currentArticulationBodyOriginalXDriveForceLimit;

    public void OnInteractionTriggerDetectorEnter(InteractionTriggerDetector itd)
    {
        currentlyIntersectingInteractionTriggers.Add(itd);
    }
    public void OnInteractionTriggerDetectorExit(InteractionTriggerDetector itd)
    {
        currentlyIntersectingInteractionTriggers.Remove(itd);
    }

    void Start()
    {
        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                var indexTipIdx = (int)OVRSkeleton.BoneId.Hand_IndexTip;
                if (indexTipIdx < ovrSkeleton.Bones.Count)
                {
                    var indexBone = ovrSkeleton.Bones[indexTipIdx];
                    indexTipMarker.position = indexBone.Transform.position;
                    indexTipMarker.rotation = indexBone.Transform.rotation;
                }
            });

        Observable.EveryUpdate()
            .Select(_ => ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            .DistinctUntilChanged()
            .Subscribe(x => 
            {
                lastPinchMarker.gameObject.SetActive(x);
                if (x)
                {
                    if (currentlyIntersectingInteractionTriggers.Any())
                    {
                        var it = currentlyIntersectingInteractionTriggers.First();
                        lastPinchMarker.SetParent(it.transform, true);
                        if (it.GetComponentInParent<ArticulationBody>() is var ab && ab)
                        {
                            if (ab.transform.parent.GetComponentInParent<ArticulationBody>() is var parentAb && parentAb && !parentAb.isRoot)
                            {
                                currentArticulationBody = parentAb;
                                currentArticulationBodyOriginalXDriveForceLimit = parentAb.xDrive.forceLimit;
                            }
                        }
                    }
                    else
                    {
                        lastPinchMarker.SetParent(null, true);
                    }
                }
                else
                {
                    lastPinchMarker.SetParent(indexTipMarker, true);
                    lastPinchMarker.localPosition = Vector3.zero;
                    lastPinchMarker.localRotation = Quaternion.identity;

                    if (currentArticulationBody)
                    {
                        var xd = currentArticulationBody.xDrive;
                        xd.forceLimit = currentArticulationBodyOriginalXDriveForceLimit;
                        xd.target = currentArticulationBody.jointPosition[0] * Mathf.Rad2Deg;
                        currentArticulationBody.xDrive = xd;
                        currentArticulationBody = null;
                    }
                }
            });
    }

    void Update()
    {
        if (lastPinchMarker.gameObject.activeSelf)
        {
            if (currentArticulationBody)
            {
                var xd = currentArticulationBody.xDrive;
                xd.forceLimit = 0.0f;
                currentArticulationBody.xDrive = xd;
                var forceDir = indexTipMarker.position - lastPinchMarker.position;
                currentArticulationBody.AddForceAtPosition(forceDir * forceMultiplier, lastPinchMarker.position);
            }
        }
    }
}
