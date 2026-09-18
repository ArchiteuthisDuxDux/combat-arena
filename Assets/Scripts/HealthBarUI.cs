using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private FighterHealth health;
    [SerializeField] private RectTransform fillBar;

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += UpdateBar;
    }

    private void Start()
    {
        if (health != null)
            UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(int currentHealth, int maxHealth)
    {
        if (fillBar == null || maxHealth <= 0)
            return;

        float value = Mathf.Clamp01((float)currentHealth / maxHealth);

        Vector3 scale = fillBar.localScale;
        scale.x = value;
        fillBar.localScale = scale;
    }
}