using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnBox : MonoBehaviour
{
    private Vector3 respawnPosition;
    private Vector3 respawnRotation;


    private void Start()
    {
        respawnPosition = transform.position;
        respawnRotation = transform.eulerAngles;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponentInParent<PlayerControl>().UpdateRespawnInformation(respawnPosition, respawnRotation);
        }
    }

    private void OnDrawGizmosSelected()
    {
        float arrowBodyLength = 3;
        float arrowHeadLength = 1;
        float arrowHeadAngle = 30;
        Vector3 pos = transform.position;
        Vector3 direction = transform.forward;

        Gizmos.DrawRay(pos, direction * arrowBodyLength);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction * arrowBodyLength, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction * arrowBodyLength, left * arrowHeadLength);
    }
}
