using UnityEngine;



public class Character : MonoBehaviour
{

    [SerializeField] private GameManager gameManager;

    [SerializeField] private GameObject character;
    [SerializeField] private ParticleSystem deathParticles;


    private void OnCollisionEnter(Collision collision)
    {

        // Only collide with vehicles if we're not already done so.

        if (collision.gameObject.CompareTag("Vehicle") && character.activeSelf)
        {

            // Hide the character model
            character.SetActive(false);
            //orient the death particles to face the collision 
            Vector3 collisionPoint = collision.GetContact(0).point;
            deathParticles.transform.position = collisionPoint;
            deathParticles.transform.LookAt(transform.position+ Vector3.up);


            deathParticles.Play();


            // Tell the GameManager we've collided.

            gameManager.PlayerCollision();

        }
    }
    public void Reset()
    {
        // Show the character model
        character.SetActive(true);
        deathParticles.Clear();
    }
}