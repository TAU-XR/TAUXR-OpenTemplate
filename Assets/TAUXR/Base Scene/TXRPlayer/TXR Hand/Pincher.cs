using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Pincher : MonoBehaviour
{
    public float Strength
    {
        get { return _pinchStrength; }
        set { _pinchStrength = value; }
    }

    [Header("Pincher Position")]
    public Transform stablePincher; // Transform to store the calculated pincher position

    private OVRSkeleton _ovrSkeleton;
    private const int INDEX_I = 20, THUMB_I = 19;
    private const float STABLE_OFFSET = 0.022f; // meters from index knuckle toward thumb

    private float _pinchStrength;
    private float _pinchDistance;
    private PinchManager _pinchManager;

    // cached bone transforms
    private Transform _indexTipT;
    private Transform _thumbTipT;
    private Transform _indexKnuckleT;

    public void Init(OVRSkeleton ovrSkeleton, PinchManager pinchManager)
    {
        _ovrSkeleton = ovrSkeleton;
        _pinchManager = pinchManager;
        CacheBones(); // try once now
    }

    private void CacheBones()
    {
        if (_ovrSkeleton == null || _ovrSkeleton.Bones == null || _ovrSkeleton.Bones.Count == 0)
            return;

        // tips
        _indexTipT = _ovrSkeleton.Bones[INDEX_I]?.Transform;
        _thumbTipT = _ovrSkeleton.Bones[THUMB_I]?.Transform;

        // walk from index tip up to the proximal knuckle (tip -> distal -> intermediate -> proximal)
        _indexKnuckleT = _indexTipT;
        if (_indexKnuckleT != null && _indexKnuckleT.parent != null) _indexKnuckleT = _indexKnuckleT.parent;           // distal
        if (_indexKnuckleT != null && _indexKnuckleT.parent != null) _indexKnuckleT = _indexKnuckleT.parent;           // intermediate
        if (_indexKnuckleT != null && _indexKnuckleT.parent != null) _indexKnuckleT = _indexKnuckleT.parent;           // proximal (knuckle)
    }

    public void UpdatePincher()
    {
        // lazy retry if bones weren't ready at Init time (OVR can populate a frame later)
        if (_indexTipT == null || _thumbTipT == null || _indexKnuckleT == null)
        {
            CacheBones();
            if (_indexTipT == null || _thumbTipT == null || _indexKnuckleT == null) return;
        }

        Vector3 indexFingerPosition = _indexTipT.position;
        Vector3 thumbFingerPosition = _thumbTipT.position;

        // Calculate pincher position as the middle point between index and thumb
        Vector3 pincherTargetPosition = (indexFingerPosition + thumbFingerPosition) * 0.5f;

        // Calculate stable pincher position: offset from index knuckle toward thumb tip
        Vector3 knucklePos = _indexKnuckleT.position;
        Vector3 dirToThumb = thumbFingerPosition - knucklePos;
        if (dirToThumb.sqrMagnitude > 1e-8f) dirToThumb.Normalize();
        Vector3 stablePincherPosition = knucklePos + dirToThumb * STABLE_OFFSET;

        // Update the stable pincher transform if assigned
        if (stablePincher != null)
        {
            stablePincher.position = stablePincherPosition;
        }

        transform.position = pincherTargetPosition;

        // strength exactly as before (uses your manager config and squared distance)
        _pinchDistance = (indexFingerPosition - thumbFingerPosition).sqrMagnitude;
        _pinchStrength = Mathf.InverseLerp(
            _pinchManager.Configuration.PinchMaxDistance,
            _pinchManager.Configuration.PinchMinDistance,
            _pinchDistance
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out APinchable pinchable)) return;
        Debug.Log("Adding pinchable: " + pinchable.name);
        _pinchManager.AddPinchableInRange(pinchable);
    }
}
