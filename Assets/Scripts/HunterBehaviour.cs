using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HunterBehaviour : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;

    private enum HunterState { Idle, Walking, Falling, Hunting }
    private HunterState state = HunterState.Idle;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float huntingSpeed = 5f;
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float catchDistance = 2f;

    [Header("Targeting")]
    [SerializeField] private LayerMask komodoLayer = 1; // Layer untuk komodo
    [SerializeField] private float targetUpdateInterval = 0.5f; // Update target setiap 0.5 detik

    private Vector3 targetPosition;
    private Transform targetKomodo; // Reference ke komodo yang sedang dikejar
    private Coroutine huntingCoroutine;

    // Flag untuk melacak apakah ini hunter pertama
    private static bool isFirstHunterSpawned = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Tambahkan komponen jika tidak ada
        if (animator == null)
        {
            Debug.LogWarning("Animator tidak ditemukan, menambahkan komponen baru");
            animator = gameObject.AddComponent<Animator>();
        }

        if (rb == null)
        {
            Debug.LogWarning("Rigidbody tidak ditemukan, menambahkan komponen baru");
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    void Start()
    {
        // Konfigurasi rigidbody
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Debug.Log("Rigidbody configured: useGravity=" + rb.useGravity + ", isKinematic=" + rb.isKinematic);
        }

        // Konfigurasi animator
        if (animator != null)
        {
            ValidateAnimatorParameters();

            // Reset state animator
            animator.SetBool("isWalking", false);
            animator.SetBool("isFalling", false);

            // Pastikan animator aktif
            animator.enabled = true;

            Debug.Log("Animator configured: hasController=" +
                     (animator.runtimeAnimatorController != null) +
                     ", speed=" + animator.speed +
                     ", enabled=" + animator.enabled);

            if (!isFirstHunterSpawned)
            {
                isFirstHunterSpawned = true;
                StartCoroutine(DelayedAnimatorRefresh());
            }
        }
    }

    IEnumerator DelayedAnimatorRefresh()
    {
        yield return null;
        yield return null;

        if (animator != null && (state == HunterState.Walking || state == HunterState.Hunting))
        {
            bool currentWalkState = animator.GetBool("isWalking");
            animator.SetBool("isWalking", !currentWalkState);
            yield return null;
            animator.SetBool("isWalking", true);

            Debug.Log("DelayedAnimatorRefresh: Refreshing walking animation state");
        }
    }

    void ValidateAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator tidak memiliki RuntimeAnimatorController!");
            return;
        }

        bool hasWalkingParam = false;
        bool hasFallingParam = false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "isWalking") hasWalkingParam = true;
            if (param.name == "isFalling") hasFallingParam = true;
        }

        if (!hasWalkingParam) Debug.LogError("Parameter 'isWalking' tidak ditemukan di Animator!");
        if (!hasFallingParam) Debug.LogError("Parameter 'isFalling' tidak ditemukan di Animator!");
    }

    void Update()
    {
        // Bergerak sesuai state
        if (state == HunterState.Walking)
        {
            MoveToTarget(walkSpeed);
        }
        else if (state == HunterState.Hunting)
        {
            MoveToTarget(huntingSpeed);

            // Cek apakah sudah sampai ke komodo
            if (targetKomodo != null)
            {
                float distanceToKomodo = Vector3.Distance(transform.position, targetKomodo.position);
                if (distanceToKomodo <= catchDistance)
                {
                    CatchKomodo();
                }
            }
        }

        // Debug logging
        if (Time.frameCount % 120 == 0) // Setiap ~2 detik
        {
            if (animator != null)
            {
                Debug.Log(gameObject.name + " - State: " + state +
                          ", Animation: isWalking=" + animator.GetBool("isWalking") +
                          ", Target: " + (targetKomodo != null ? targetKomodo.name : "None"));
            }
        }
    }

    void MoveToTarget(float speed)
    {
        // Bergerak ke target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Rotasi menghadap ke arah gerakan
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    // Method untuk memulai berburu komodo terdekat
    public void StartHuntingKomodo()
    {
        Debug.Log("StartHuntingKomodo called for " + gameObject.name);

        // Cari komodo terdekat
        Transform nearestKomodo = FindNearestKomodo();

        if (nearestKomodo != null)
        {
            StartHuntingSpecificKomodo(nearestKomodo);
        }
        else
        {
            Debug.LogWarning("Tidak ada komodo yang ditemukan dalam radius deteksi!");
            // Fallback ke behavior lama jika tidak ada komodo
            StartWalking(GetRandomPosition());
        }
    }

    // Method untuk berburu komodo spesifik
    public void StartHuntingSpecificKomodo(Transform komodo)
    {
        if (komodo == null) return;

        Debug.Log(gameObject.name + " mulai berburu " + komodo.name);

        state = HunterState.Hunting;
        targetKomodo = komodo;
        targetPosition = komodo.position;

        // Setup rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Setup animator
        SetupWalkingAnimation();

        // Mulai coroutine untuk update target position
        if (huntingCoroutine != null)
        {
            StopCoroutine(huntingCoroutine);
        }
        huntingCoroutine = StartCoroutine(UpdateTargetPosition());
    }

    // Coroutine untuk terus update posisi target komodo
    IEnumerator UpdateTargetPosition()
    {
        while (state == HunterState.Hunting && targetKomodo != null)
        {
            // Update target position ke posisi komodo saat ini
            targetPosition = targetKomodo.position;

            // Cek apakah komodo masih dalam jangkauan
            float distanceToKomodo = Vector3.Distance(transform.position, targetKomodo.position);
            if (distanceToKomodo > detectionRadius * 2) // Jika terlalu jauh, berhenti berburu
            {
                Debug.Log(gameObject.name + " kehilangan jejak " + targetKomodo.name);
                StartHuntingKomodo(); // Cari komodo terdekat lagi
                yield break;
            }

            yield return new WaitForSeconds(targetUpdateInterval);
        }
    }

    // Method untuk mencari komodo terdekat
    Transform FindNearestKomodo()
    {
        // Cari semua objek dengan komponen KomodoPatrol
        KomodoPatrol[] allKomodos = FindObjectsOfType<KomodoPatrol>();

        if (allKomodos.Length == 0)
        {
            Debug.LogWarning("Tidak ada komodo yang ditemukan di scene!");
            return null;
        }

        Transform nearestKomodo = null;
        float nearestDistance = float.MaxValue;

        foreach (KomodoPatrol komodo in allKomodos)
        {
            float distance = Vector3.Distance(transform.position, komodo.transform.position);

            // Cek apakah dalam radius deteksi
            if (distance <= detectionRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestKomodo = komodo.transform;
            }
        }

        if (nearestKomodo != null)
        {
            Debug.Log(gameObject.name + " menemukan komodo terdekat: " + nearestKomodo.name +
                     " pada jarak " + nearestDistance.ToString("F1") + "m");
        }

        return nearestKomodo;
    }

    // Method ketika berhasil menangkap komodo
    void CatchKomodo()
    {
        Debug.Log(gameObject.name + " berhasil menangkap " + targetKomodo.name + "!");

        // Bisa tambahkan efek atau suara di sini

        // Mulai falling sequence
        StartFalling();
    }

    // Method fallback untuk walking ke posisi random (behavior lama)
    public void StartWalking(Vector3 target)
    {
        Debug.Log("StartWalking called for " + gameObject.name);

        state = HunterState.Walking;
        targetPosition = target;
        targetKomodo = null; // Reset target komodo

        // Arahkan menghadap target
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Set rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        SetupWalkingAnimation();
    }

    void SetupWalkingAnimation()
    {
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("RuntimeAnimatorController missing! Animation won't work!");
            }

            animator.enabled = true;
            if (animator.speed <= 0) animator.speed = 1f;

            // Set triggers
            if (HasParameter("Walk", AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger("Fall");
                animator.SetTrigger("Walk");
            }

            // Set bool parameters
            animator.SetBool("isWalking", true);
            animator.SetBool("isFalling", false);

            Debug.Log("Animator set for walking: isWalking=true, speed=" + animator.speed);

            StartCoroutine(EnsureWalkingAnimation());
        }
    }

    IEnumerator EnsureWalkingAnimation()
    {
        yield return new WaitForSeconds(0.1f);

        if (animator != null && (state == HunterState.Walking || state == HunterState.Hunting))
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") &&
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
            {
                Debug.Log("Forcing walking animation refresh");

                animator.SetBool("isWalking", false);
                yield return null;
                animator.SetBool("isWalking", true);

                if (HasParameter("Walk", AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger("Walk");
                }
            }
        }
    }

    public void StartFalling()
    {
        state = HunterState.Falling;

        // Stop hunting coroutine
        if (huntingCoroutine != null)
        {
            StopCoroutine(huntingCoroutine);
            huntingCoroutine = null;
        }

        Debug.Log("Starting falling sequence for " + gameObject.name);

        // Set rigidbody untuk jatuh
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
        }

        // Set animator untuk animasi jatuh
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isFalling", true);

            if (HasParameter("Fall", AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger("Walk");
                animator.SetTrigger("Fall");
            }

            if (animator.speed == 0) animator.speed = 1f;
        }

        // Hancurkan object setelah beberapa waktu
        Destroy(gameObject, 2f);
    }

    // Helper method untuk posisi random (fallback)
    Vector3 GetRandomPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f;
        randomDirection.y = 0; // Keep on ground level
        randomDirection += transform.position;
        return randomDirection;
    }

    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
            {
                return true;
            }
        }
        return false;
    }

    // Gizmos untuk debug
    void OnDrawGizmosSelected()
    {
        // Gambar radius deteksi
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Gambar garis ke target
        if (targetKomodo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetKomodo.position);
        }

        // Gambar radius tangkap
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}