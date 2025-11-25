using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    //public ItemData item;
    public Inventory inventory;
    public Block block;

    public int amount;

    [Header("UI Reference")]
    public Image itemIcon;                          // 아이템 아이콘 이미지
    public Text amountText;                         // 개수 텍스트
    public GameObject emptySlotImage;               // 빈 슬롯 일 때 보여줄 이미지

    // [추가됨] 슬롯의 투명도를 조절하기 위한 컴포넌트
    public CanvasGroup slotCanvasGroup;

    public Image slotBackgroundImage;

    // Start is called before the first frame update
    void Start()
    {
        // 시작할 때 CanvasGroup 컴포넌트를 자동으로 찾도록 시도 (인스펙터 할당 실수 방지)
        if (slotCanvasGroup == null)
            slotCanvasGroup = GetComponent<CanvasGroup>();
        if (slotBackgroundImage == null) slotBackgroundImage = GetComponent<Image>();
        UpdateSlotUI();
    }

    public void SetItem(Block newBlock, int newAmount)        // 슬롯에 아이템 설정
    {
        block = newBlock;
        amount = newAmount;
        UpdateSlotUI();
    }

    void UpdateSlotUI()
    {
        if (block != null) // 아이템이 있을 때
        {
            // 1. 기존 UI 업데이트
            itemIcon.sprite = block.itemIcon;
            itemIcon.enabled = true;

            amountText.text = amount > 1 ? amount.ToString() : "";

            if (emptySlotImage != null)
                emptySlotImage.SetActive(false);

            // 2. [추가됨] 슬롯을 보이게 설정 (Alpha 1)
            if (slotCanvasGroup != null)
            {
                slotCanvasGroup.alpha = 1f;           // 완전 불투명
                slotCanvasGroup.interactable = true;  // 클릭 가능
                slotCanvasGroup.blocksRaycasts = true; // 레이캐스트 감지
            }
        }
        else // 아이템이 없을 때 (빈 슬롯)
        {
            // 1. 기존 UI 업데이트
            itemIcon.enabled = false;
            amountText.text = "";

            if (emptySlotImage != null)
                emptySlotImage.SetActive(true);

            // 2. [추가됨] 슬롯을 안 보이게 설정 (Alpha 0)
            if (slotCanvasGroup != null)
            {
                slotCanvasGroup.alpha = 0f;           // 완전 투명
                slotCanvasGroup.interactable = false; // 클릭 불가능
                slotCanvasGroup.blocksRaycasts = false; // 레이캐스트 무시
            }
        }
    }

    public void AddAmount(int value)        // 아이템 개수 추가
    {
        amount += value;
        UpdateSlotUI();
    }

    public void RemoveAmount(int value)     // 아이템 개수 제거
    {
        amount -= value;
        if (amount <= 0)
        {
            ClearSlot();
        }
        else
        {
            UpdateSlotUI();
        }
    }

    public void ClearSlot()      // 슬롯을 비우는 함수
    {
        block = null;
        amount = 0;
        UpdateSlotUI();
        SetSelectionState(false);
    }

    public void SetSelectionState(bool isSelected)
    {
        if (slotBackgroundImage != null)
        {
            if (isSelected)
            {
                slotBackgroundImage.color = Color.yellow; // 선택되면 노란색
            }
            else
            {
                slotBackgroundImage.color = Color.white;  // 평소에는 흰색 (또는 원래 색상)
            }
        }
    }
}