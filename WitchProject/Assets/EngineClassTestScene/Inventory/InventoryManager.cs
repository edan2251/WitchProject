using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Setting")]
    public int inventorySize = 6;      //인벤토리 슬롯 개수
    public GameObject inventoryUI;      //UI 패널
    public Transform itemSlotParanet;   //슬롯들이 들어갈 부모 오브젝트
    public GameObject itemSlotPrefab;   //슬롯 프리팹

    [Header("Input")]
    public KeyCode inventoryKey = KeyCode.I;                            //인벤토리 열기 키
    public List<InventorySlot> slots = new List<InventorySlot>();       //모든 슬롯 리스트
    private bool isInventoryOpen = false;                               //인벤토리가 열려있는지 확인

    private int selectedSlotIndex = -1;

    public Block CurrentSelectedBlock
    {
        get
        {
            if (selectedSlotIndex == -1) return null;
            return slots[selectedSlotIndex].block;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    // Start is called before the first frame update
    void Start()
    {
        CreateInventorySlots();
        inventoryUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(inventoryKey)) // 'I' 키 (인벤토리 열기)
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ClearAllInventory();
        }

        CheckSelectionInput();
    }

    void CheckSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
    }

    public void SelectSlot(int index)
    {
        // 인덱스 범위를 벗어나거나, 해당 슬롯에 아이템이 없으면 선택 불가
        if (index >= slots.Count || slots[index].block == null)
        {
            // (옵션) 아이템이 없는 빈칸을 눌렀을 때 기존 선택을 해제하고 싶다면 아래 주석 해제
            // SelectSlot(-1); 
            return;
        }

        // 이미 선택된 슬롯을 다시 누르면 선택 해제 (Toggle)
        if (selectedSlotIndex == index)
        {
            selectedSlotIndex = -1; // 선택 해제 상태로 변경
        }
        else
        {
            selectedSlotIndex = index; // 새로운 슬롯 선택
        }

        UpdateSelectionVisual(); // 시각적 업데이트
    }

    void UpdateSelectionVisual()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == selectedSlotIndex)
            {
                slots[i].SetSelectionState(true); // 노란색
            }
            else
            {
                slots[i].SetSelectionState(false); // 흰색
            }
        }
    }

    void CreateInventorySlots()         //인벤토리 슬롯들을 생성하는 함수
    {
        for(int i = 0; i < inventorySize; i++)
        {
            GameObject slotObject = Instantiate(itemSlotPrefab, itemSlotParanet);
            InventorySlot slot = slotObject.GetComponent<InventorySlot>();
            slots.Add(slot);            //리스트에 추가
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);

    }

    public bool AddItem(Block blockToAdd, int amount = 1)
    {
        foreach (InventorySlot slot in slots)
        {
            if (slot.block != null && slot.block.type == blockToAdd.type && slot.amount < blockToAdd.maxStack)
            {
                int spaceLeft = blockToAdd.maxStack - slot.amount; 
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                slot.AddAmount(amountToAdd);

                amount -= amountToAdd;

                if (amount <= 0)
                {
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot.block == null)
            {
                slot.SetItem(blockToAdd, amount); 
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 참");
        return false;
    }

    public void RemoveItem(Block item, int amount = 1)
    {
        foreach(InventorySlot slot in slots)
        {
            if(slot.block == item)
            {
                slot.RemoveAmount(amount);
                return;
            }
        }
    }

    public int GetItemCount(Block item)
    {
        int count = 0;
        foreach (InventorySlot slot in slots)
        {
            if(slot.block == item)
            {
                count += slot.amount;
            }
        }
        return count;
    }

    public void ClearAllInventory()
    {
        foreach (InventorySlot slot in slots)
        {
            slot.ClearSlot(); // 각 슬롯의 ClearSlot() 함수 호출
        }

        selectedSlotIndex = -1;
        UpdateSelectionVisual();

        Debug.Log("인벤토리 전체가 초기화되었습니다.");
    }

    public void ConsumeSelectedOne()
    {
        if (selectedSlotIndex == -1) return;

        InventorySlot currentSlot = slots[selectedSlotIndex];

        if (currentSlot.block != null)
        {
            currentSlot.RemoveAmount(1);

            // 만약 다 써서 슬롯이 비워졌다면, 시각적으로 선택 상태 갱신 (선택은 유지하되 내용은 비움)
            if (currentSlot.block == null)
            {
                // 필요하다면 여기서 선택 해제 로직을 넣을 수도 있음
                // SelectSlot(-1); 
            }
        }
    }
}
