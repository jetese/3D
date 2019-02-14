using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionScript : MonoBehaviour
{

    private Animator anim;

    private float rotation = 100.0f;
    public float damping = 0.15f;

    //Selector de idles
    int idleSelector = 1;
    //Selector random de caras
    int FaceSelector = 1;
    //¿Estamos en idle?
    bool idle = true;
    //¿Estamos agachados?
    bool down = false;

    //Variables que guardan los valores del Update anterior
    float verticalAntiguo = 0;
    float horizontalAntiguo = 0;

    //Tiempo para entrar en el Idle especial
    public float timeToIdle = 30.0f; 

    //Tiempo actual 
    float currentTime = 0f;
    //Tiempo que llevamos en el Idle
    float idleTime = 0f;

    // Use this for initialization
    void Start()
    {
        anim = this.GetComponent<Animator>();
        idleSelector = (int)Random.Range(1f, 3.99f);
        FaceSelector = (int)Random.Range(1f, 4.99f);
        anim.SetInteger("IdleSelector", idleSelector);
        anim.SetInteger("FaceSelector", FaceSelector);
        anim.SetBool("isIdle", idle);
    }

    // Update is called once per frame
    void Update()
    {
        //Obtenemos los movimientos
        float horizontal = Input.GetAxis("Horizontal") * rotation * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical");

        //Seteamos la rotacion en el personaje
        this.transform.Rotate(0, horizontal, 0);

        //Obtenemos si estamos agachados
        down = anim.GetBool("isDown");

        //Obtenemos el tiempo actual
        currentTime = Time.time;

        // Si nos movimos en el estado anterior pero ahora no y no estamos agachados
        if ((verticalAntiguo != 0 || horizontalAntiguo != 0) && vertical == 0 && horizontal == 0 && !down)
        {
            idleSelector = (int)Random.Range(1f, 3.99f);                                    //Seleccionamos un idle de los tres que tenemos
            anim.SetInteger("IdleSelector", idleSelector);
            FaceSelector = (int)Random.Range(1f, 4.99f);                                    //Seleccionamos la cara
            anim.SetInteger("FaceSelector", FaceSelector);
            idle = true;                                                                    //Activamos idle
            anim.SetBool("isIdle", idle);                                                   
            
            idleTime = currentTime;                                                         //Comienza la cuenta del idle especial
        } 
        // Si no nos hemos movido en un tiempo, estamos en idle y el tiempo es > 30
        else if (vertical == 0 && horizontal == 0 && idle == true && (currentTime - idleTime) > timeToIdle)
        {
            FaceSelector = (int)Random.Range(1f, 4.99f);                                    //Seleccionamos la cara
            anim.SetInteger("FaceSelector", FaceSelector);
            idleSelector = 4;                                                               //Pasamos al idle especial
            anim.SetInteger("IdleSelector", idleSelector);
        } 
        // Si nos movemos
        else if (vertical != 0 ||  horizontal != 0)
        {
            idle = false;                                                                   //Dejamos de estar en idle                                           
            anim.SetBool("isIdle", idle);
            
        }

        //Si pulsamos Alt y no estamos agachados pero si andando
        if (Input.GetKey(KeyCode.V) && !down && vertical != 0)                                           
        {
            anim.SetBool("isStealth", true);                                                 //Entramos en sigilo
        }
        //Si levantamos el dedo de Z o no estamos andando
        if (Input.GetKeyUp(KeyCode.V) || vertical == 0)
        {
            anim.SetBool("isStealth", false);                                                //Dejamos de estar en sigilo
        }

        //Si estamos andando normal
        else
        {
            //Si pulsamos Shift corremos
            if (Input.GetKey(KeyCode.LeftShift))
            {
                vertical *= 2;
                //Si pulsamos espacio, saltamos 
                if (Input.GetKeyDown(KeyCode.Space) && !down)
                {
                    anim.SetBool("isJumping", true);
                    Invoke("StopJumping", 0.4f); // Cuando acabe la animación, dejamos de saltar
                }
            }
        }

            
        //Si pulsamos c y no estamos agachados
        if (Input.GetKeyDown(KeyCode.C) && !down)
        {
            idle = false;                                                //Dejamos de estar en idle por si lo estabamos y nos agachamos
            anim.SetBool("isIdle", idle);
            anim.SetBool("isDown", true);
        }
        //Si pulsamos c y estamos agachados
        else if (Input.GetKeyDown(KeyCode.C) && down)
        {
            idleSelector = (int)Random.Range(1f, 3.99f);                                    //Seleccionamos un idle de los tres que tenemos
            anim.SetInteger("IdleSelector", idleSelector);
            FaceSelector = (int)Random.Range(1f, 4.99f);                                    //Seleccionamos la cara
            anim.SetInteger("FaceSelector", FaceSelector);
            idle = true;                                                // Nos levantamos y entramos en idle
            anim.SetBool("isIdle", idle);
            idleTime = currentTime;
            anim.SetBool("isDown", false);
        }

        //Seteamos las variables del animator de los ejes
        anim.SetFloat("Horizontal", horizontal);
        anim.SetFloat("Vertical", vertical, damping, Time.deltaTime);

        //Seteamos los estados antiguos
        verticalAntiguo = vertical;
        horizontalAntiguo = horizontal;
    }

    //Dejamos de saltar
    void StopJumping()
    {
        anim.SetBool("isJumping", false);
    }
}
