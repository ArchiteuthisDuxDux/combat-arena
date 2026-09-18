using UnityEngine;

public class FighterVisual : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;

    public void SetColor(Color color)
    {
        if (bodyRenderer == null) return;

        bodyRenderer.material.color = color;
    }
}