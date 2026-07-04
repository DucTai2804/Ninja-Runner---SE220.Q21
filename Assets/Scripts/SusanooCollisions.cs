using UnityEngine;

// --- SCRIPT CHO THÂN THỂ SUSANOO ---
public class SusanooBodyCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.name.Contains("GiantRockSlide") || other.name.Contains("GiantRockJump") || other.name.Contains("MountainWall"))
        {
            string objName = other.gameObject.name;
            // Nếu đâm bằng thân vào chướng ngại vật siêu bự, bị trừ thời gian
            if (objName.Contains("MountainWall") || objName.Contains("GiantRock"))
            {
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.ReduceSusanooTime();
                    Debug.Log("Susanoo Body hit large obstacle: Penalty applied.");
                }
            }
            else
            {
                Debug.Log("Susanoo Body crushed an obstacle.");
            }
            other.gameObject.SetActive(false);
        }
    }
}

// --- SCRIPT CHO LƯỠI KIẾM SUSANOO ---
public class SusanooSwordCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.name.Contains("GiantRockSlide") || other.name.Contains("GiantRockJump") || other.name.Contains("MountainWall"))
        {
            if (SkillManager.Instance != null && SkillManager.Instance.IsSlashing())
            {
                Debug.Log("Susanoo Sword perfectly slashed the obstacle!");
                other.gameObject.SetActive(false);
            }
        }
    }
}
