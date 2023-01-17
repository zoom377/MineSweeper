using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileButton : MonoBehaviour, IPointerUpHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            SendOnTileClicked();
        else if (eventData.button == PointerEventData.InputButton.Right)
            SendOnTileFlagged();
    }

    public void SendOnTileClicked()
    {
        var mineSweeper = GameObject.Find("MineSweeper").GetComponent<MineSweeper>();
        mineSweeper.OnTileClicked(gameObject);
    }

    public void SendOnTileFlagged()
    {
        var mineSweeper = GameObject.Find("MineSweeper").GetComponent<MineSweeper>();
        mineSweeper.OnTileFlagged(gameObject);
    }
}
