using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelWIn : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnClose;
    private UIMainManager m_mainManager;
    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (btnClose) btnClose.onClick.RemoveAllListeners();
    }
    public void Setup(UIMainManager mainManager)
    {
        m_mainManager = mainManager;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnClickClose()
    {
        m_mainManager.ShowMainMenu();
    }
}

