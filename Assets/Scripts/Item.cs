using Unity.VisualScripting;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float MinPrice, MaxPrice;
    public float Price = 0f;

    public int MinKg, MaxKg;
    public int kg = 0;
    private Range_Interaction ROI;
    public PlayerManager playerManagerl;
    void Awake()
    {
        MaxKg = MaxKg <= MinKg ? MinKg + 10 : MaxKg;
        MaxPrice = MaxPrice <= MinPrice ? MinPrice + 10 : MaxPrice;
        ROI = GetComponentInChildren<Range_Interaction>();
        kg = Random.Range(MinKg, MaxKg);
        Price = Random.Range(MinPrice, MaxPrice);   
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("home"))
        {
            playerManagerl.currpoint += (int)Price;
            Debug.Log($"Curr: {playerManagerl.currpoint} / {playerManagerl.totalpoint}");
            Destroy(gameObject);
        }
    }

}
