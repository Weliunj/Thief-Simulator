using TMPro;
using UnityEngine;

/// <summary>
/// Component gắn vào vùng giao hàng / điểm đích (Home / Van / Truck):
/// - Tự động phát hiện khi bất kỳ vật phẩm (tag "item" hoặc chứa component Item) rơi/ném vào vùng bán.
/// - Hiển thị 3D Billboard Text "DROP ITEM HERE" lơ lửng và xoay hướng theo Camera.
/// - Sau khi có đủ 3 vật phẩm được thả/bán vào vùng này, tự động ẩn dòng chữ hướng dẫn.
/// - Thực hiện bán an toàn, chống kích hoạt trùng lặp nhiều lần (chống x2 điểm).
/// </summary>
[RequireComponent(typeof(Collider))]
public class HomeSellZone : MonoBehaviour
{
    [Header("🎯 Tag Configuration")]
    [Tooltip("Tag của vật phẩm hợp lệ để bán (mặc định là 'item')")]
    public string targetItemTag = "item";

    [Header("🔊 Audio")]
    [Tooltip("Âm thanh phát khi bán thành công (nếu có)")]
    public AudioClip sellSuccessAudio;

    [Header("📝 Floating 'Drop Item Here' 3D Text")]
    [Tooltip("TextMeshPro 3D hiển thị chữ hướng dẫn (nếu để trống sẽ tự tạo)")]
    public TextMeshPro dropHereText3D;
    public Transform textDisplayAnchor;
    public Vector3 textOffset = new Vector3(0f, 1.8f, 0f);
    public string promptText = "▼ DROP ITEM HERE ▼";
    public Color promptColor = new Color(0.3f, 1f, 1f, 1f); // Xanh Cyan sáng

    [Tooltip("Số lượng vật phẩm cần thả vào để ẩn dòng chữ (mặc định 3 item)")]
    public int hideAfterItemCount = 3;

    [SerializeField] private int droppedItemCount = 0;

    private AudioSource _audioSource;

    private void Awake()
    {
        // Đảm bảo Collider là Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (sellSuccessAudio != null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0.5f;
            }
        }

        Ensure3DText();
    }

    /// <summary>
    /// Tự động khởi tạo 3D Text "DROP ITEM HERE" nếu chưa có
    /// </summary>
    private void Ensure3DText()
    {
        if (dropHereText3D == null)
        {
            dropHereText3D = GetComponentInChildren<TextMeshPro>(true);
        }

        if (dropHereText3D == null)
        {
            GameObject textObj = new GameObject("DropItemHere_3DText");
            textObj.transform.SetParent(textDisplayAnchor != null ? textDisplayAnchor : transform, false);
            textObj.transform.localPosition = textOffset;

            dropHereText3D = textObj.AddComponent<TextMeshPro>();
            dropHereText3D.alignment = TextAlignmentOptions.Center;
            dropHereText3D.fontSize = 5f;
            dropHereText3D.fontStyle = FontStyles.Bold;
            dropHereText3D.color = promptColor;
            dropHereText3D.text = promptText;
        }
        else
        {
            dropHereText3D.text = promptText;
            dropHereText3D.color = promptColor;
        }
    }

    private void LateUpdate()
    {
        // Billboard Effect: Dòng chữ luôn xoay mặt hướng thẳng về Camera của người chơi
        if (dropHereText3D != null && dropHereText3D.gameObject.activeSelf)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                dropHereText3D.transform.rotation = mainCam.transform.rotation;
            }
        }
    }

    /// <summary>
    /// Ghi nhận 1 vật phẩm đã được thả vào vùng bán. Khi đủ 3 vật phẩm sẽ ẩn text
    /// </summary>
    public void RegisterItemDropped()
    {
        droppedItemCount++;
        if (droppedItemCount >= hideAfterItemCount)
        {
            if (dropHereText3D != null && dropHereText3D.gameObject.activeSelf)
            {
                dropHereText3D.gameObject.SetActive(false);
                Debug.Log($"[HomeSellZone] Đã thả đủ {droppedItemCount}/{hideAfterItemCount} vật phẩm -> Ẩn chữ hướng dẫn.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // 1. Kiểm tra ưu tiên theo component Item hoặc tag
        Item itemComp = other.GetComponent<Item>() ?? other.GetComponentInParent<Item>();
        bool isItem = itemComp != null;
        if (!isItem)
        {
            try
            {
                isItem = (!string.IsNullOrEmpty(targetItemTag) && other.gameObject.tag == targetItemTag) || other.gameObject.tag == "item";
            }
            catch { }
        }

        if (itemComp != null || isItem)
        {
            if (itemComp != null && !itemComp.IsSold)
            {
                itemComp.SellItem();
                PlaySellSound();
                RegisterItemDropped();
            }
            else if (itemComp == null && other.gameObject != null)
            {
                // Fallback nếu object chỉ có tag 'item' mà chưa gắn Item component
                Debug.LogWarning($"[HomeSellZone] Phát hiện Object '{other.name}' có tag '{targetItemTag}' nhưng không có component Item.");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null) return;

        Item itemComp = collision.gameObject.GetComponent<Item>() ?? collision.gameObject.GetComponentInParent<Item>();
        bool isItem = itemComp != null;
        if (!isItem)
        {
            try
            {
                isItem = (!string.IsNullOrEmpty(targetItemTag) && collision.gameObject.tag == targetItemTag) || collision.gameObject.tag == "item";
            }
            catch { }
        }

        if (itemComp != null && !itemComp.IsSold)
        {
            itemComp.SellItem();
            PlaySellSound();
            RegisterItemDropped();
        }
    }

    private void PlaySellSound()
    {
        if (_audioSource != null && sellSuccessAudio != null)
        {
            _audioSource.PlayOneShot(sellSuccessAudio);
        }
    }
}
