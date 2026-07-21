using UnityEngine;
using UnityEngine.UI;

public class UiButton : MonoBehaviour
{
    private void Start()
    {
        if (TryGetComponent(out Button button))
            button.onClick.AddListener(() => UiAudioController.Instance.PlayPressButtonSound());
    }
}
