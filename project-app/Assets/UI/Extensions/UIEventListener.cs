/*
 * Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 24/02/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: 
 */


using UnityEngine;
using UnityEngine.EventSystems;


	public class UIEventListener : EventTrigger
	{
		public System.Action onClick;
	
		public System.Action<PointerEventData> onPointerDown;
		public System.Action<PointerEventData> onPointerEnter;
		public System.Action<PointerEventData> onPointerExit;
		public System.Action<PointerEventData> onPointerUp;

		public System.Action<PointerEventData> onBeginDrag;
		public System.Action<PointerEventData> onEndDrag;
		public System.Action<PointerEventData> onDrag;

		public System.Action<bool> onValueChanged;

		public static UIEventListener Get(GameObject go)
		{
			UIEventListener listener = go.GetComponent<UIEventListener>();
			if (listener == null) listener = go.AddComponent<UIEventListener>();

			return listener;
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			if (onClick != null) onClick();
		}
		public override void OnPointerDown(PointerEventData eventData)
		{
			if (onPointerDown != null) onPointerDown(eventData);
		}
		public override void OnPointerEnter(PointerEventData eventData)
		{
			if (onPointerEnter != null) onPointerEnter(eventData);
		}
		public override void OnPointerExit(PointerEventData eventData)
		{
			if (onPointerExit != null) onPointerExit(eventData);
		}
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (onPointerUp != null) onPointerUp(eventData);
		}
		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (onBeginDrag != null) onBeginDrag(eventData);
		}
		public override void OnEndDrag(PointerEventData eventData)
		{
			if (onEndDrag != null) onEndDrag(eventData);
		}
		public override void OnDrag(PointerEventData eventData) 
		{
			if (onDrag != null) onDrag(eventData);
		}

	    void OnDestroy()
	    {
	        onClick = null;

	        onPointerDown = null;
	        onPointerEnter = null;
	        onPointerExit = null;
	        onPointerUp = null;

	        onBeginDrag = null;
	        onEndDrag = null;
	        onDrag = null;

	        onValueChanged = null;
	    }
	}

