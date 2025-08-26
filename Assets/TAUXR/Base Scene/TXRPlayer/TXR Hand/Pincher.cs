using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Pincher : MonoBehaviour
{
    //public float Strength => _pinchStrength;
    public float Strength
    {
        get { return _pinchStrength; }
        set { _pinchStrength = value; }
    }using System;
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

        // stable pincher aim: offset from index knuckle toward thumb tip
        Vector3 knucklePos = _indexKnuckleT.position;
        Vector3 dirToThumb = thumbFingerPosition - knucklePos;
        if (dirToThumb.sqrMagnitude > 1e-8f) dirToThumb.Normalize();
        Vector3 pincherTargetPosition = knucklePos + dirToThumb * STABLE_OFFSET;

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


    private OVRSkeleton _ovrSkeleton;
    private const int INDEX_I = 20, THUMB_I = 19;
    private float _pinchStrength;

    private float _pinchDistance;

    private PinchManager _pinchManager;

    public void Init(OVRSkeleton ovrSkeleton, PinchManager pinchManager)
    {
        _ovrSkeleton = ovrSkeleton;
        _pinchManager = pinchManager;
    }

    public void UpdatePincher()
    {
        // set pinch position
        Vector3 indexFingerPosition = _ovrSkeleton.Bones[INDEX_I].Transform.position;
        Vector3 thumbFingerPosition = _ovrSkeleton.Bones[THUMB_I].Transform.position;
        Vector3 pincherTargetPosition = (thumbFingerPosition + indexFingerPosition) / 2;
        transform.position = pincherTargetPosition;

        // set pinch strength based on finger distance
        _pinchDistance = (indexFingerPosition - thumbFingerPosition).sqrMagnitude;
        _pinchStrength = Mathf.InverseLerp(_pinchManager.Configuration.PinchMaxDistance, _pinchManager.Configuration.PinchMinDistance,
            _pinchDistance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out APinchable pinchable))
        {
            return;
        }

        Debug.Log("Adding pinchable: " + pinchable.name);
        _pinchManager.AddPinchableInRange(pinchable);
    }
}