using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeTela {

    #region InEditorVariables

    public float Mass;

    #endregion
    //public float Mass;
    public bool Fixed;

    public Vector3 Pos;
    public Vector3 Vel;
    public Vector3 Force;

    public MassSpringCloth Manager;
    public NodeTela(float masa, Vector3 posicion, bool fijado)
    {
        Mass = masa;
        Pos = posicion;
        Fixed = fijado;
    }
	
    public void ActualizarPos(Vector3 posicion)
    {
        Pos = posicion;
    }
	// Update is called once per frame
	public Vector3 ObtenerPos() {
        return Pos;
	}

    public void AddForces(float alpha)
    {
        Force += Mass * Manager.Gravity;
        Force -= Mass * alpha * Vel;
    }

    public void AddForce(Vector3 fuerza)
    {
        Force += fuerza;
    }
}
