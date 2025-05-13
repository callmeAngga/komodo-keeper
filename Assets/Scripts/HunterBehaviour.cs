using UnityEngine;
using System.Collections;

public class HunterBehaviour : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;

    private enum HunterState { Idle, Walking, Falling }
    private HunterState state = HunterState.Idle;

    [SerializeField] private float walkSpeed = 2f;
    private Vector3 targetPosition;
    
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
            // Verifikasi parameter animator
            ValidateAnimatorParameters();
            
            // Reset state animator
            animator.SetBool("isWalking", false);
            animator.SetBool("isFalling", false);
            
            // Pastikan animator aktif
            animator.enabled = true;
            
            // Debug info
            Debug.Log("Animator configured: hasController=" + 
                     (animator.runtimeAnimatorController != null) + 
                     ", speed=" + animator.speed + 
                     ", enabled=" + animator.enabled);

            // PENTING: Jika ini hunter pertama, berikan waktu untuk animator menginisialisasi
            if (!isFirstHunterSpawned)
            {
                isFirstHunterSpawned = true;
                // Delay singkat untuk memastikan animator terinisialisasi dengan baik
                StartCoroutine(DelayedAnimatorRefresh());
            }
        }
    }

    // Coroutine untuk memastikan animator terinisialisasi dengan baik pada hunter pertama
    IEnumerator DelayedAnimatorRefresh()
    {
        // Tunggu dua frame agar Unity mengupdate sistem animasi
        yield return null;
        yield return null;
        
        // Refresh animator state
        if (animator != null && state == HunterState.Walking)
        {
            // Toggle state untuk memaksa refresh
            bool currentWalkState = animator.GetBool("isWalking");
            animator.SetBool("isWalking", !currentWalkState);
            yield return null; // Tunggu satu frame
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
        // Hunter bergerak ke target
        if (state == HunterState.Walking)
        {
            // Bergerak ke target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);
            
            // Rotasi menghadap ke arah gerakan
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            // Debug logging
            if (Time.frameCount % 120 == 0) // Setiap ~2 detik
            {
                if (animator != null)
                {
                    Debug.Log(gameObject.name + " - Walking animation state: isWalking=" + 
                              animator.GetBool("isWalking") + 
                              ", animator enabled=" + animator.enabled +
                              ", animator speed=" + animator.speed);
                }
            }
        }
    }

    public void StartWalking(Vector3 target)
    {
        Debug.Log("StartWalking called for " + gameObject.name);
        
        state = HunterState.Walking;
        targetPosition = target;

        // Arahkan menghadap target
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Set rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;  // Gunakan velocity, bukan linearVelocity
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            Debug.Log("Rigidbody set for walking: useGravity=false, isKinematic=true");
        }

        // Set animator
        if (animator != null)
        {
            // PENTING: Pastikan animator aktif dan memiliki controller
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("RuntimeAnimatorController missing! Animation won't work!");
            }
            
            // Pastikan animator aktif
            animator.enabled = true;
            
            // Set speed
            if (animator.speed <= 0) animator.speed = 1f;
            
            // PENTING: Set state dengan Trigger juga selain Bool
            // Ini akan memaksa transisi di state machine
            if (HasParameter("Walk", AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger("Fall");
                animator.SetTrigger("Walk");
            }
            
            // Set bool parameters
            animator.SetBool("isWalking", true);
            animator.SetBool("isFalling", false);
            
            Debug.Log("Animator set for walking: isWalking=true, speed=" + animator.speed);
            
            // PENTING: Mulai coroutine untuk memastikan animasi berjalan
            StartCoroutine(EnsureWalkingAnimation());
        }
    }
    
    // Coroutine untuk memastikan animasi walking berjalan
    IEnumerator EnsureWalkingAnimation()
    {
        // Tunggu sebentar untuk memastikan animasi sudah diproses
        yield return new WaitForSeconds(0.1f);
        
        // Periksa apakah animator masih dalam kondisi yang benar
        if (animator != null && state == HunterState.Walking)
        {
            // Jika tetap tidak berjalan, coba paksa refresh
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") && 
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
            {
                Debug.Log("Forcing walking animation refresh");
                
                // Toggle parameter untuk memaksa perubahan state
                animator.SetBool("isWalking", false);
                yield return null;
                animator.SetBool("isWalking", true);
                
                // Set trigger jika ada
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
        
        Debug.Log("Starting falling sequence for " + gameObject.name);

        // Set rigidbody untuk jatuh
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // PENTING: Aktifkan fisica untuk jatuh
            rb.useGravity = true;
            rb.isKinematic = false;
            
            // Berikan dorongan ke bawah
            rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
            
            Debug.Log("Rigidbody set for falling: useGravity=true, isKinematic=false");
        }

        // Set animator untuk animasi jatuh
        if (animator != null)
        {
            // PENTING: Perbaiki bug - isWalking harus FALSE, bukan true
            animator.SetBool("isWalking", false);
            animator.SetBool("isFalling", true);
            
            // Gunakan trigger jika ada
            if (HasParameter("Fall", AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger("Walk");
                animator.SetTrigger("Fall");
            }
            
            // Pastikan animator aktif
            if (animator.speed == 0) animator.speed = 1f;
            
            Debug.Log("Animator set for falling: isWalking=false, isFalling=true");
        }
        else
        {
            Debug.LogError("Animator tidak ditemukan saat mencoba memulai animasi falling");
        }

        // Hancurkan object setelah beberapa waktu
        Destroy(gameObject, 2f);
    }
    
    // Helper method untuk memeriksa parameter di animator
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
}