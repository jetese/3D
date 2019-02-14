using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring  {

    #region InEditorVariables

    public float Stiffness;
    public Node nodeA;
    public Node nodeB;

    #endregion

    public float Length0;
    public float Length;
    private float volume;

    public MassSpringSolid Manager;

    public  Spring(Node a, Node b, float dureza, float vol)
    {
        volume = vol;
        nodeA = a;
        nodeB = b;
        Length0 =  Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
        Stiffness = dureza;
    }

    public void AddVolume (float vol)
    {
        volume += vol;
    }

    public void ActualizarLongitud()
    {
        Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
    }

    public void AddForces(float beta)
    {
        Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
        Vector3 Force = new Vector3();

        Force = (-volume/(Length0 *Length0)) * Stiffness * (Length - Length0) * (nodeA.ObtenerPos() - nodeB.ObtenerPos()) / Length;

        nodeA.AddForce(Force);
        nodeB.AddForce(-Force);

    }
}
