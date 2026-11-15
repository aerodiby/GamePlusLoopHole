using UnityEngine;

namespace UI.MainMenuUI
{
    public class MainMenuBackdropMotion : MonoBehaviour
    {
        [Header("Camera Parallax")]
        public Transform cameraTransform;
        [Range(0f, 0.15f)] public float parallaxAmount = 0.045f;
        public float parallaxSmoothness = 3f;

        private Vector3 _startPosition;
        private Vector3 _cameraStartPosition;
        private Transform[] _floatingObjects;
        private Vector3[] _floatingPositions;
        private float[] _phases;
        private float[] _amplitudes;
        private float[] _drift;

        private void Awake()
        {
            _startPosition = transform.position;
            if (cameraTransform != null)
                _cameraStartPosition = cameraTransform.position;

            int count = transform.childCount;
            _floatingObjects = new Transform[count];
            _floatingPositions = new Vector3[count];
            _phases = new float[count];
            _amplitudes = new float[count];
            _drift = new float[count];
            for (int i = 0; i < count; i++)
            {
                Transform item = transform.GetChild(i);
                _floatingObjects[i] = item;
                _floatingPositions[i] = item.position;
                _phases[i] = i * 1.73f;
                _amplitudes[i] = item.name.StartsWith("Cloud") ? 0.09f : item.name.Contains("Balloon") ? 0.16f : item.name.Contains("gem") ? 0.08f : 0.025f;
                _drift[i] = item.name.StartsWith("Cloud") ? 0.08f : 0f;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector3 delta = cameraTransform.position - _cameraStartPosition;
            Vector3 target = _startPosition + new Vector3(delta.x, delta.y, 0f) * parallaxAmount;
            transform.position = Vector3.Lerp(transform.position, target, Time.unscaledDeltaTime * parallaxSmoothness);

            float time = Time.unscaledTime;
            for (int i = 0; i < _floatingObjects.Length; i++)
            {
                if (_floatingObjects[i] == null) continue;
                Vector3 position = _floatingPositions[i];
                position.y += Mathf.Sin(time * 0.65f + _phases[i]) * _amplitudes[i];
                position.x += Mathf.Sin(time * 0.22f + _phases[i]) * _drift[i];
                _floatingObjects[i].position = position;
            }
        }
    }
}
