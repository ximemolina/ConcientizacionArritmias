using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls the steps in the coaching card.
    /// Each step can have its own AudioClip, played via GameAudioManager when the step becomes visible.
    /// </summary>
    public class TutoManager : MonoBehaviour
    {
        [Serializable]
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;

            [SerializeField]
            public string buttonText2;

            [Tooltip("Voiceover that plays when this step becomes visible. Leave empty for silence.")]
            [SerializeField]
            public AudioClip stepAudio;
        }

        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField;
        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField2;

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

        [Header("Second Button (shown on last step)")]
        [SerializeField]
        GameObject m_SecondButton;

        [Header("Object to deactivate on second button press")]
        [SerializeField]
        GameObject m_ObjectToDeactivate;

        [Header("Optional object to activate on second button press")]
        [SerializeField]
        GameObject[] m_ObjectsToActivate;

        [Header("Script opcional a inicializar tuto")]
        public LanzadorTutorial lanzadorTutorial;

        [Header("Script opcional partida 30 seg")]
        public LanzadorObjetos lanzadorObjetos;

        [Header("Script opcional timer 30 seg")]
        public Timer timer;

        [Header("Audio conteo regresivo")]
        public AudioSource conteoRegresivo;

        int m_CurrentStepIndex = 0;

        public List<AudioClip> frameVoiceovers;

        // -------------------------------------------------------------------------

        void Start()
        {
            Debug.Log("StepManager iniciado correctamente");

            bool isFirstStep = m_CurrentStepIndex == 0;
            SetSecondButtonVisible(!isFirstStep);

            if (!isFirstStep && m_StepButtonTextField2 != null)
                m_StepButtonTextField2.text = m_StepList[m_CurrentStepIndex].buttonText2;

            // Play audio for the first step on start
            PlayCurrentStepAudio();
        }

        // -------------------------------------------------------------------------
        // Navigation
        // -------------------------------------------------------------------------

        public void Next()
        {

            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = (m_CurrentStepIndex + 1) % m_StepList.Count;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;

            if (m_StepButtonTextField2 != null)
                m_StepButtonTextField2.text = m_StepList[m_CurrentStepIndex].buttonText2;

            Debug.Log($"Step actual: {m_CurrentStepIndex} / {m_StepList.Count - 1}");
            GameAudioManager.Instance?.PlayTutorialAudio(frameVoiceovers[m_CurrentStepIndex]);
            bool isFirstStep = m_CurrentStepIndex == 0;
            SetSecondButtonVisible(!isFirstStep);

            PlayCurrentStepAudio();
        }

        public void Previous()
        {
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = (m_CurrentStepIndex - 1 + m_StepList.Count) % m_StepList.Count;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
            GameAudioManager.Instance?.PlayTutorialAudio(frameVoiceovers[m_CurrentStepIndex]);
            m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
            m_StepButtonTextField2.text = m_StepList[m_CurrentStepIndex].buttonText2;

            Debug.Log($"Step actual: {m_CurrentStepIndex} / {m_StepList.Count - 1}");

            bool isFirstStep = m_CurrentStepIndex == 0;
            SetSecondButtonVisible(!isFirstStep);

            PlayCurrentStepAudio();
        }

        public void ReiniciarTutorial()
        {
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex++;

            if (m_CurrentStepIndex < m_StepList.Count)
            {
                m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
                m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;

                Debug.Log($"Step actual: {m_CurrentStepIndex} / {m_StepList.Count - 1}");

                bool isLastStep = m_CurrentStepIndex == m_StepList.Count - 1;
                SetSecondButtonVisible(isLastStep);

                PlayCurrentStepAudio();
            }
            else
            {
                m_CurrentStepIndex = 0;
                m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
                m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
                SetSecondButtonVisible(false);

                Debug.Log("Reiniciado al primer card antes de desactivar.");

                PlayCurrentStepAudio();

                if (m_ObjectToDeactivate != null)
                    deactivateObject();

                if (m_ObjectsToActivate != null)
                    activateObjects();
            }
        }

        public void ResetToFirstCard()
        {
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = 0;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
            SetSecondButtonVisible(false);

            Debug.Log("Tutorial reiniciado al primer card.");

            PlayCurrentStepAudio();
        }

        // -------------------------------------------------------------------------
        // Button handlers (unchanged)
        // -------------------------------------------------------------------------

        public void OnFirstButtonPressed()
        {
            SetSecondButtonVisible(false);
            Next();
        }

        public void OnSecondButtonPressed()
        {
            deactivateObject();
            activateObjects();
        }

        public void OnSecondButtonPressed2()
        {
            m_ObjectToDeactivate.SetActive(false);
            activateObjects();
            activateObjects();
        }

        public void OnBotonPresionado1()
        {
            if (m_CurrentStepIndex == 0)
                Next();
            //else if (m_CurrentStepIndex == m_StepList.Count - 1)
                //Next();
            else
                Previous();
        }

        public void OnBotonPresionado2()
        {
            Debug.Log($"OnBotonPresionado2 llamado. CurrentStepIndex: {m_CurrentStepIndex}, ÚltimoStep: {m_StepList.Count - 1}");

            if (m_CurrentStepIndex == m_StepList.Count - 1)
            {
                OnIniciarButtonPressed();
            }
            else
                Next();
        }

        public void OnBotonPresionado3()
        {
            if (m_CurrentStepIndex == m_StepList.Count - 1)
                OnSecondButtonPressed();
            else
                Next();
        }

        public void OnIniciarButtonPressed()
        {
            Debug.Log("Iniciar button pressed - iniciando partida");
            deactivateObject();
            StopCurrentStepAudio();
            StartCoroutine(IniciarConConteo());
        }

        public void OnIniciarPartidaRealPressed()
        {
            deactivateObject();
            StartCoroutine(IniciarPartidaRealConConteo());
        }

        // -------------------------------------------------------------------------
        // Audio
        // -------------------------------------------------------------------------

        private void PlayCurrentStepAudio()
        {
            if (GameAudioManager.Instance == null) return;

            var clip = m_StepList[m_CurrentStepIndex].stepAudio;
            if (clip != null)
                GameAudioManager.Instance.PlayTutorialAudio(clip);
        }

        private void StopCurrentStepAudio()
        {
            if (GameAudioManager.Instance == null) return;
            
            var clip = m_StepList[m_CurrentStepIndex].stepAudio;
            if (clip != null)
                GameAudioManager.Instance.StopTutorialAudio(clip);
        }

        // -------------------------------------------------------------------------
        // Coroutines & helpers (unchanged)
        // -------------------------------------------------------------------------

        private IEnumerator IniciarConConteo()
        {
            if (lanzadorTutorial != null)
            {
                Debug.Log("Iniciando tutorial");
                ResetToFirstCard();
                lanzadorTutorial.Relanzar();
            }
            else if (lanzadorObjetos != null)
            {
                if (conteoRegresivo != null && conteoRegresivo.clip != null)
                {
                    conteoRegresivo.Play();
                    // Esperar la duración real del clip
                    yield return new WaitForSeconds(conteoRegresivo.clip.length);
                }
                else
                {
                    Debug.LogWarning("AudioSource conteoRegresivo no asignado o sin clip.");
                    yield return new WaitForSeconds(3f); // fallback
                }

                Debug.Log("Iniciando partida de 60 segundos");
                timer.IniciarContador(60f);
                lanzadorObjetos.Relanzar(60f);
                //StartCoroutine(ActivarDespues(60f));
            }
            else
            {
                Debug.LogWarning("No se asignó el SegundoScript en el Inspector.");
            }
        }

        private IEnumerator IniciarPartidaRealConConteo()
        {
            if (lanzadorObjetos != null)
            {
                if (conteoRegresivo != null && conteoRegresivo.clip != null)
                {
                    conteoRegresivo.Play();
                    // Esperar la duración real del clip
                    yield return new WaitForSeconds(conteoRegresivo.clip.length);
                }
                else
                {
                    Debug.LogWarning("AudioSource conteoRegresivo no asignado o sin clip.");
                    yield return new WaitForSeconds(3f); // fallback
                }
                Debug.Log("Iniciando partida de 60 segundos");
                timer.IniciarContador(60f);
                lanzadorObjetos.Relanzar(60f);
            }
            else
            {
                Debug.LogWarning("No se asignó el SegundoScript en el Inspector.");
            }
        }

        private IEnumerator ActivarDespues(float tiempo)
        {
            yield return new WaitForSeconds(tiempo);
            activateObjects();
        }

        void SetSecondButtonVisible(bool visible)
        {
            if (m_SecondButton == null)
            {
                Debug.LogWarning("m_SecondButton no está asignado.");
                return;
            }
            m_SecondButton.SetActive(visible);
            Debug.Log($"Segundo botón SetActive: {visible}");
        }

        void activateObjects()
        {
            if (m_ObjectsToActivate != null && m_ObjectsToActivate.Length > 0)
            {
                foreach (GameObject obj in m_ObjectsToActivate)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        Debug.Log($"Objeto activado: {obj.name}");

                        SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
                        if (smr != null)
                        {
                            smr.enabled = true;
                            Debug.Log($"SkinnedMeshRenderer activado en: {obj.name}");
                        }

                        Transform child = obj.transform.Find("CoachingCardRoot");
                        if (child != null)
                        {
                            child.gameObject.SetActive(true);
                            Debug.Log("Objeto hijo 'CoachingCardRoot' activado.");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No se asignaron objetos en la lista m_ObjectsToActivate.");
            }
        }

        void deactivateObject()
        {
            if (m_ObjectToDeactivate != null)
            {
                SkinnedMeshRenderer smr = m_ObjectToDeactivate.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.enabled = false;
                    Debug.Log($"SkinnedMeshRenderer desactivado en: {m_ObjectToDeactivate.name}");
                }
                else
                {
                    Debug.LogWarning($"No se encontró SkinnedMeshRenderer en: {m_ObjectToDeactivate.name}");
                }

                Transform child = m_ObjectToDeactivate.transform.Find("CoachingCardRoot");
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    Debug.Log("Objeto hijo 'CoachingCardRoot' desactivado.");
                }
                else
                {
                    Debug.LogWarning("No se encontró un hijo llamado 'CoachingCardRoot'.");
                }
            }
            else
            {
                Debug.LogWarning("m_ObjectToDeactivate no está asignado.");
            }
        }
    }
}