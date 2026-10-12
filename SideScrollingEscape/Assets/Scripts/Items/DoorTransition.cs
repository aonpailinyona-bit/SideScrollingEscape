using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Player;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SideScrollingEscape.Items
{
    /// <summary>
    /// A simple passage door that teleports the player between areas.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DoorTransition : MonoBehaviour, IInteractable
    {
        [Header("Connection")]
        [Tooltip("The destination door that the player will teleport to (optional fallback).")]
        [SerializeField] private DoorTransition targetDoor;

        [Tooltip("The exact spawn point transform the player will be teleported to.")]
        [SerializeField] private Transform destination;

        [Header("Visuals & Audio")]
        [Tooltip("Audio clip played when transitioning through the door.")]
        [SerializeField] private AudioClip transitionClip;

        [Tooltip("Prompt displayed to the player.")]
        [SerializeField] private string promptText = "Press E to Enter Room";

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private bool transitioning;
        private static float globalDoorCooldownUntil;

        public bool CanInteract(GameObject interactor)
        {
            return !transitioning && Time.unscaledTime >= globalDoorCooldownUntil &&
                (destination != null || (targetDoor != null && targetDoor != this));
        }

        public void Interact(GameObject interactor)
        {
            if (transitioning || Time.unscaledTime < globalDoorCooldownUntil ||
                (destination == null && (targetDoor == null || targetDoor == this)))
            {
                return;
            }

            PlayerController controller = interactor.GetComponent<PlayerController>();
            if (controller != null && !controller.BeginTransition()) return;
            transitioning = true;
            globalDoorCooldownUntil = Time.unscaledTime + 0.55f;

            Camera outputCamera = Camera.main;
            float orthographicSizeBeforeTeleport = outputCamera != null
                ? outputCamera.orthographicSize
                : -1f;

            // Print the current destination info to Console as requested by user
            string destName = destination != null ? destination.name : (targetDoor != null ? targetDoor.gameObject.name : "None");
            Vector3 destPos = destination != null ? destination.position : (targetDoor != null ? targetDoor.transform.position : Vector3.zero);
            Debug.Log("[DoorTransition] " + gameObject.name + " teleporting " + interactor.name + " to destination '" + destName + "' at world position " + destPos);

            // Play transition sound
            if (transitionClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAt(transitionClip, transform.position, 1f);
            }

            // Reset velocity of Rigidbody2D if exists
            if (interactor.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Teleport the interactor
            Vector3 targetPos = destination != null ? destination.position : targetDoor.transform.position;
            
            // Calculate delta before teleportation for camera warping
            Vector3 delta = targetPos - interactor.transform.position;

            if (rb != null)
            {
                rb.position = targetPos;
                rb.transform.position = targetPos; // Instantly sync transform position
            }
            else
            {
                interactor.transform.position = targetPos;
            }

            // Reset velocity again after teleport to prevent any carried over velocity
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Handle Camera Teleportation and Confiner Switching
            if (interactor.CompareTag("Player"))
            {
                var vcamObj = GameObject.Find("CM_VCam");
                if (vcamObj != null)
                {
                    var vcam = vcamObj.GetComponent<CinemachineCamera>();
                    if (vcam != null)
                    {
                        // Re-assert the player target after every teleport. This
                        // also repairs cameras whose serialized target was lost.
                        vcam.Follow = interactor.transform;

                        // Preserve exactly the same zoom that was visible before
                        // teleporting instead of letting the new bounds affect it.
                        if (orthographicSizeBeforeTeleport > 0f)
                        {
                            vcam.Lens.OrthographicSize = orthographicSizeBeforeTeleport;
                            if (outputCamera != null)
                                outputCamera.orthographicSize = orthographicSizeBeforeTeleport;
                        }

                        // 1. Switch Confiner Bounds based on target location
                        var confiner = vcamObj.GetComponent<CinemachineConfiner2D>();
                        if (confiner != null)
                        {
                            GameObject boundsObj = null;
                            bool isLevel3 = SceneManager.GetActiveScene().name == "Level3"
                                || GameObject.Find("Tilemap_2") != null;

                            if (isLevel3)
                            {
                                boundsObj = targetPos.y > 15f
                                    ? GetOrCreateLevel3UpperBounds()
                                    : GameObject.Find("CameraBounds");
                            }
                            else if (targetPos.y > 15f)
                            {
                                if (targetPos.x < 20f)
                                {
                                    boundsObj = GameObject.Find("Bounds_Room_A");
                                }
                                else
                                {
                                    boundsObj = GameObject.Find("Bounds_Room_B");
                                }
                            }
                            else
                            {
                                boundsObj = GameObject.Find("Bounds_Corridor");
                            }

                            if (boundsObj != null)
                            {
                                var col = boundsObj.GetComponent<Collider2D>();
                                if (col != null)
                                {
                                    confiner.BoundingShape2D = col;
                                    confiner.InvalidateBoundingShapeCache();
                                }
                            }
                        }

                        // 2. Warp Virtual Camera and prevent smooth panning lag/snapping delay
                        vcam.OnTargetObjectWarped(interactor.transform, delta);
                        vcam.PreviousStateIsValid = false;
                    }
                }
            }

            StartCoroutine(FinishTransition(controller));
        }

        private IEnumerator FinishTransition(PlayerController controller)
        {
            yield return new WaitForSecondsRealtime(0.18f);
            controller?.EndTransition();
            transitioning = false;
        }

        private static GameObject GetOrCreateLevel3UpperBounds()
        {
            const string boundsName = "Level3_UpperCameraBounds";
            GameObject boundsObj = GameObject.Find(boundsName);
            if (boundsObj == null)
            {
                boundsObj = new GameObject(boundsName);
                boundsObj.AddComponent<PolygonCollider2D>();
            }

            var polygon = boundsObj.GetComponent<PolygonCollider2D>();
            polygon.isTrigger = true;
            polygon.SetPath(0, new[]
            {
                new Vector2(56.5f, 44.35f),
                new Vector2(103.5f, 44.35f),
                new Vector2(103.5f, 68.5f),
                new Vector2(56.5f, 68.5f)
            });

            return boundsObj;
        }

        public string GetPrompt()
        {
            return Time.unscaledTime < globalDoorCooldownUntil ? "Please wait..." : promptText;
        }
    }
}
