using UnityEngine;

public class Leak : Interactable
{
    public BoatLeakManager boatLeakManager;

    private float fixingProgressMax = 5f;
    private float fixingProgress = 0f;
    private float decayProgess = -0.3f;
    private float fixingRate = 0.5f;
    private bool isFixing;
    [SerializeField] private GameObject leakUI;

    public void Update()
    {
        float progress = 0;
        if (isFixing) { progress = fixingRate; }
        else { if (fixingProgress > 0) { progress = decayProgess; } }
        fixingProgress += progress * Time.deltaTime;

        if (fixingProgress <= 0f)
        {
            leakUI.SetActive(false);
            return;
        }
        else if (fixingProgress > 0f) {
            leakUI.SetActive(true);
        }
    }

    public override void Interact(Player player)
    {

        if (player.HasObjectPickUp())
        {
            if (player.GetObjectPickUp().TryGetComponent<TapeController>(out TapeController tapeController))
            {
                isFixing = true;
                Debug.Log("Fixing");
            }
            
            //The player is holding something
            if(fixingProgress >= fixingProgressMax)
            {
                DestroySelf();
            }
            

        }
        else
        {
            // The player is not holding something
            Debug.Log("Player has no item");
            return;
        }
    }

    public override void Cancel(Player player)
    {
        isFixing = false;
       
    }

    public void DestroySelf()
    {
        boatLeakManager.RepairLeak();
        Destroy(gameObject);
    }
}
