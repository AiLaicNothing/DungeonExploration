using UnityEngine;

public class ChangePlayer : MonoBehaviour
{
    public GameObject knight;
    public GameObject mage;

    public int index = 0;
    public void changePlayer()
    {
        if(index == 0)
        {
            index = 1;
        }
        else if (index == 1)
        {
            index = 0;
        }
    }

    private void Update()
    {
        if (index == 0)
        {
            knight.SetActive(false);
            mage.SetActive(true);

        }
        else if (index == 1)
        {
            mage.SetActive(false);
            knight.SetActive(true);
        }
    }
}
