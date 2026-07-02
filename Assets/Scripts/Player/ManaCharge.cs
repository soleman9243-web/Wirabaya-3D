using UnityEngine;
using System.Reflection;

public class ManaCharge : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode chargeKey = KeyCode.C;
    public float chargeRate = 25f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private int isChargingHash;

    [Header("VFX")]
    [SerializeField] private GameObject auraEffect;

    public bool isCharging { get; private set; }

    private MonoBehaviour inputController;
    private FieldInfo moveField;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        isChargingHash = Animator.StringToHash("isCharging");

        if (auraEffect != null)
        {
            auraEffect.SetActive(false);
        }

        inputController = GetComponent("StarterAssetsInputs") as MonoBehaviour;
        if (inputController != null)
        {
            moveField = inputController.GetType().GetField("move");
        }
    }

    private void Update()
    {
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.health <= 0)
        {
            StopCharging();
            return;
        }

        if (Input.GetKeyDown(chargeKey))
        {
            if (PlayerStatus.Instance.mana < PlayerStatus.Instance.maxMana)
            {
                StartCharging();
            }
        }

        if (Input.GetKey(chargeKey) && isCharging)
        {
            if (moveField != null)
            {
                moveField.SetValue(inputController, Vector2.zero);
            }

            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);

            if (PlayerStatus.Instance.mana >= PlayerStatus.Instance.maxMana)
            {
                StopCharging();
            }
            else
            {
                PlayerStatus.Instance.RestoreMana(chargeRate * Time.deltaTime);
            }
        }

        if (Input.GetKeyUp(chargeKey))
        {
            StopCharging();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        animator.SetBool(isChargingHash, true);

        if (auraEffect != null)
        {
            auraEffect.SetActive(true);
        }
    }

    private void StopCharging()
    {
        if (!isCharging) return;

        isCharging = false;
        animator.SetBool(isChargingHash, false);

        if (auraEffect != null)
        {
            auraEffect.SetActive(false);
        }
    }
}