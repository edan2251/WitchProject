using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarvester : MonoBehaviour
{
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;

    [Header("Mining Settings")]
    public int toolDamage = 1;
    public float hitCooldown = 0.15f;
    private float _nextHitTime;

    [Header("Building Settings")]
    public float buildCooldown = 0.2f; // 설치 쿨타임
    private float _nextBuildTime;

    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        // 인벤토리가 열려있거나 마우스가 UI 위에 있다면 동작 중지 (필요 시 주석 해제하여 사용)
        // if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        // --- 1. 블록 파괴 (좌클릭) ---
        if (Input.GetMouseButton(0) && Time.time >= _nextHitTime)
        {
            TryMineBlock();
        }

        // --- 2. 블록 설치 (우클릭) ---
        // * 좌클릭으로 설치를 원하시면 Input.GetMouseButtonDown(0)으로 바꾸되 파괴 로직과 충돌을 주의하세요.
        if (Input.GetMouseButtonDown(1) && Time.time >= _nextBuildTime)
        {
            TryPlaceBlock();
        }
    }

    void TryMineBlock()
    {
        _nextHitTime = Time.time + hitCooldown;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
        {
            var block = hit.collider.GetComponent<Block>();
            if (block != null)
            {
                block.Hit(toolDamage);
            }
        }
    }

    void TryPlaceBlock()
    {
        // 1. 현재 인벤토리에서 선택된 블록 가져오기
        Block blockToPlace = InventoryManager.Instance.CurrentSelectedBlock;

        // 선택된 블록이 없거나, 설치할 프리팹 정보가 없으면 리턴
        if (blockToPlace == null || blockToPlace.itemPrefab == null) return;

        _nextBuildTime = Time.time + buildCooldown;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // 레이캐스트 발사
        if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
        {
            // 2. 설치할 위치 계산
            // hit.transform.position : 맞은 블록의 중심 좌표 (큐브가 정수 좌표에 있다고 가정)
            // hit.normal : 맞은 면의 방향 (예: (0,1,0)이면 윗면, (0,0,-1)이면 뒷면)

            // 큐브들이 정확히 정수 좌표에 있다면 아래 공식을 사용합니다.
            Vector3 targetPos = hit.transform.position + hit.normal;

            // 만약 큐브 위치가 소수점일 수 있다면, 반올림을 해주는 것이 안전합니다.
            // Vector3 targetPos = new Vector3(
            //     Mathf.Round(hit.point.x + hit.normal.x * 0.5f),
            //     Mathf.Round(hit.point.y + hit.normal.y * 0.5f),
            //     Mathf.Round(hit.point.z + hit.normal.z * 0.5f)
            // );

            // 3. 플레이어 위치와 겹치는지 확인 (선택 사항)
            // if (Vector3.Distance(transform.position, targetPos) < 1.0f) return; 

            // 4. 블록 생성
            // itemPrefab은 Block 스크립트 타입이므로 .gameObject로 접근하여 생성
            GameObject newBlockObj = Instantiate(blockToPlace.itemPrefab.gameObject, targetPos, Quaternion.identity);

            // 이름 정리 (선택사항)
            newBlockObj.name = $"{blockToPlace.itemName}_Placed";

            // 5. 인벤토리에서 아이템 1개 소모
            InventoryManager.Instance.ConsumeSelectedOne();
        }
    }
}