/*
 * William Gonzalez
 * AP CS50
 * Cmdr. Schenk
 * 5th Period
 * Master Project (PortChess) - Tile POCO
 * 27 April 2023
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class modeling a tile on the chess board
public class Tile : MonoBehaviour
{

    public float RotSpeed;
    public int x;
    public int y;

    public GameObject OrbitPoint; // References Sphere prim in unity scene
    public float OrbitRadius; // Radius of orbit
    public float OrbitSpeed; // Orbit speed
    public float CastRadius; // Margin of error
    public bool RandomPosAcquired;
    public LayerMask Avoid;
    private Vector3 RandomInSphere;
    public bool Orbiting;

    // Constructor
    void Start()
    {
        RotSpeed = Random.Range(73f, 192f);
        StartOrbit();
    }

    // Update is called once per frame by Unity
    void Update()
    {
        if (Orbiting) {
            Orbit();
        }
    }

    // Orbit animation
    void Orbit() {
        while (!RandomPosAcquired)
        {
            RaycastHit hit;
            RandomInSphere = Random.insideUnitSphere * OrbitRadius;
            if (!Physics.CapsuleCast(transform.position, RandomInSphere + OrbitPoint.transform.position, CastRadius, transform.forward, out hit, Mathf.Infinity, Avoid))
            {
                RandomPosAcquired = true;
            }
        }
        if (RandomPosAcquired)
        {
            if (Vector3.Distance(transform.position, RandomInSphere + OrbitPoint.transform.position) <= 0.1f)
            {
                RandomPosAcquired = false;
            }
            //transform.LookAt(RandomInSphere + OrbitPoint.transform.position);
            //transform.LookAt(OrbitPoint.transform.position, Vector3.back);
            transform.Rotate(new Vector3(RotSpeed * Time.deltaTime, RotSpeed * Time.deltaTime, RotSpeed * Time.deltaTime));
            transform.position = Vector3.Slerp(transform.position, RandomInSphere + OrbitPoint.transform.position, OrbitSpeed * Time.deltaTime);
        }
    }

    // Button Action Events
    public void StartOrbit() {
        Orbiting = true;
    }
    public void StopOrbit() {
        Orbiting = false;
    }

}
