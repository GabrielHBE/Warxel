using System;
using UnityEngine;

public static class LockInTargeting
{
    public static bool HasLineOfSight(Vector3 origin, Vector3 aimPoint, Transform shooterRoot,
        Vehicle target, LayerMask obstructionMask, ref RaycastHit[] hits)
    {
        Vector3 offset = aimPoint - origin;
        float distance = offset.magnitude;
        if (target == null || distance <= 0.0001f) return false;

        int count;
        do
        {
            count = Physics.RaycastNonAlloc(origin, offset / distance, hits,
                distance + 0.05f, obstructionMask, QueryTriggerInteraction.Ignore);
            if (count < hits.Length) break;
            Array.Resize(ref hits, hits.Length * 2);
        } while (true);

        Collider closest = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider body = hits[i].collider;
            if (body == null || (shooterRoot != null && body.transform.IsChildOf(shooterRoot))) continue;
            if (hits[i].distance >= closestDistance) continue;
            closest = body;
            closestDistance = hits[i].distance;
        }

        // A candidate must be the first physical body along the camera's sight line.
        return closest != null && closest.GetComponentInParent<Vehicle>() == target;
    }

    public static Collider FindTargetBody(Vehicle target)
    {
        if (target == null) return null;
        Collider largestBody = null;
        float largestVolume = -1f;
        foreach (Collider body in target.GetComponentsInChildren<Collider>())
        {
            if (!body.enabled || body.isTrigger ||
                body.GetComponentInParent<Vehicle>() != target ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;

            Vector3 size = body.bounds.size;
            float volume = size.x * size.y * size.z;
            if (volume <= largestVolume) continue;
            largestVolume = volume;
            largestBody = body;
        }
        return largestBody;
    }

    public static Vector3 GetAimPosition(Vehicle target, Collider body)
    {
        if (body != null && body.enabled && body.gameObject.activeInHierarchy) return body.bounds.center;
        return target.rb != null ? target.rb.worldCenterOfMass : target.transform.position;
    }

    public static Vector3 GetInterceptDirection(Vector3 missilePosition, float missileSpeed,
        Vector3 aimPosition, Vector3 targetVelocity)
    {
        Vector3 offset = aimPosition - missilePosition;
        if (missileSpeed <= 0.0001f) return offset.normalized;

        // Solve |offset + targetVelocity * time| = missileSpeed * time.
        float a = targetVelocity.sqrMagnitude - missileSpeed * missileSpeed;
        float b = 2f * Vector3.Dot(offset, targetVelocity);
        float c = offset.sqrMagnitude;
        float interceptTime = 0f;
        if (Mathf.Abs(a) <= 0.0001f)
        {
            if (Mathf.Abs(b) > 0.0001f) interceptTime = Mathf.Max(0f, -c / b);
        }
        else
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant >= 0f)
            {
                float root = Mathf.Sqrt(discriminant);
                float first = (-b - root) / (2f * a);
                float second = (-b + root) / (2f * a);
                if (first > 0f && second > 0f) interceptTime = Mathf.Min(first, second);
                else interceptTime = Mathf.Max(0f, Mathf.Max(first, second));
            }
        }

        return (offset + targetVelocity * interceptTime).normalized;
    }

    public static void Steer(Rigidbody missile, Vehicle target, Collider body, float speed,
        float turnSpeed, float deltaTime)
    {
        Vector3 aimPosition = GetAimPosition(target, body);
        Vector3 targetVelocity = target.rb != null ? target.rb.GetPointVelocity(aimPosition) : Vector3.zero;
        Vector3 direction = GetInterceptDirection(missile.position, speed, aimPosition, targetVelocity);
        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion rotation = Quaternion.RotateTowards(missile.rotation,
            Quaternion.LookRotation(direction), Mathf.Max(0f, turnSpeed) * deltaTime);
        // Use the physics pose, not an interpolated render transform.
        missile.rotation = rotation;
        missile.angularVelocity = Vector3.zero;
        missile.linearVelocity = rotation * Vector3.forward * speed;
    }
}
