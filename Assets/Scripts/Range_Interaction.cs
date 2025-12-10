using StarterAssets;
using TMPro; // Đảm bảo đã import namespace này
using UnityEngine;

public class Range_Interaction : MonoBehaviour
{
    // Cài đặt Vị trí và Bán kính kiểm tra
    [Header("Range & Status")]
    public GameObject Pivot;
    public float Radius = 3f; // Đặt giá trị mặc định hợp lý
    public bool InRange;

    // Cài đặt Hiển thị
    [Header("Display & Locking")]
    // Đã thay đổi từ TextMesh sang TMPro.TextMeshPro
    public TMPro.TextMeshPro NameDisplay; 
    public GameObject Center; // Đối tượng xoay để LookAt
    public GameObject E_icon; // Icon chữ E hoặc button
    public Color NameColor = Color.white; // Màu mặc định

    private GameObject _target;
    // Third Person Controller Reference
    private ThirdPersonController _thirdPersonController;

    void Start()
    {
        E_icon.SetActive(false);
        
        // 1. Khởi tạo TextMeshPro
        if (NameDisplay != null)
        {
            // Gán tên đối tượng cha (Object mà script này gắn vào)
            NameDisplay.text = transform.parent.gameObject.name;     
            // Áp dụng màu sắc
            NameDisplay.color = NameColor;
        }
        else
        {
            Debug.LogError("TextMeshPro component (NameDisplay) not found or not assigned in the Inspector.");
        }

        // 2. Tìm kiếm Controller và Camera
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _thirdPersonController = player.GetComponent<ThirdPersonController>();
            if (_thirdPersonController == null)
            {
                Debug.LogError("ThirdPersonController not found on Player");
            }
        }
        else
        {
            Debug.LogError("Player GameObject with tag 'Player' not found.");
        }

        _target = GameObject.FindGameObjectWithTag("MainCamera");
        if (_target == null)
        {
            Debug.LogError("MainCamera GameObject with tag 'MainCamera' not found.");
        }
    }

    void Update()
    {
        // Kiểm tra Player trong phạm vi
        CheckPlayerInRange();

        if (InRange)
        {
            E_Interact(); // Hiển thị icon tương tác
            LookAtObject(); // Xoay đối tượng hiển thị về phía camera
        }
        else
        {
            E_icon.SetActive(false);
        }

        // TODO: Thêm logic xử lý đầu vào (Input) để tương tác ở đây
        // Ví dụ: 
        // if (InRange && Input.GetKeyDown(KeyCode.E))
        // {
        //     // Gọi hàm tương tác của đối tượng này
        //     Debug.Log("Player interacted with: " + transform.parent.gameObject.name);
        // }
    }
    
    // --- Các hàm hỗ trợ ---

    private void CheckPlayerInRange()
    {
        // Đảm bảo Pivot đã được gán trước khi kiểm tra
        if (Pivot == null)
        {
            Debug.LogError("Pivot GameObject is not assigned in the Inspector.");
            InRange = false;
            return;
        }

        // Sử dụng LayerMask để kiểm tra chỉ Player Layer
        // Lưu ý: Đảm bảo Layer "Player" đã được thiết lập đúng
        InRange = Physics.CheckSphere(Pivot.transform.position, Radius, LayerMask.GetMask("Player"));
    }

    public void E_Interact()
    {
        E_icon.SetActive(true);
    }
    
    public void LookAtObject()
    {
        if (Center == null || _target == null) return;
        
        // Tính vector hướng từ Center đến Camera (target)
        Vector3 direc = _target.transform.position - Center.transform.position;
        
        // Xoay Center để nhìn vào Camera
        Center.transform.rotation = Quaternion.LookRotation(direc);
    }

    private void OnDrawGizmos()
    {
        // Vẽ Gizmo chỉ khi trong Editor và Pivot đã được gán
        if (Pivot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Pivot.transform.position, Radius);
        }
    }
}