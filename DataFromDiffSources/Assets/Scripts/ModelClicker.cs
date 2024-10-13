using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ModelClicker : MonoBehaviour
{
    [SerializeField]
    private LayerMask interactableLayer;

    [SerializeField]
    private UnityEvent OnClicked;


    private Camera mainCam;
    private void Awake()
    {
        mainCam = Camera.main;
    }
    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                var ray = mainCam.ScreenPointToRay(Input.mousePosition);
                var distance = float.MaxValue;

                if (Physics.Raycast(ray, out var hit, distance, interactableLayer))
                {
                    if (hit.collider.gameObject.TryGetComponent<ModelPump>(out _))
                    {
                        OnClicked?.Invoke();
                    }
                }
            }
        }
    }
}
