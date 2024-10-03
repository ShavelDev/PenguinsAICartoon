using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;

public class VoicesManager : MonoBehaviour
{
    Dictionary<string, string> actorsCode;

    string urlJobToken = "https://api.fakeyou.com/tts/inference";
    string urlJobStatus = "https://api.fakeyou.com/tts/job/";
    string audioUrl = "https://storage.googleapis.com/vocodes-public";

    public TMP_Text completedLinesText;
    



    // Start is called before the first frame update
    void Start()
    {
        completedLinesText.text = "waiting";
        actorsCode = new Dictionary<string, string>();
        actorsCode.Add("kowalski", "weight_z6k2h74bztvcmxfe86n790ghm");
        actorsCode.Add("skipper", "weight_dj2h3kg80ccmv0mmyvvjc4xzj");
        //actorsCode.Add("skipper", "weight_6mzytdp5s5e957t5ksjf16ynp");
        actorsCode.Add("private", "weight_ncam4cc0awpzswy5dxv44s960");


        /*
        AudioSource audioSource = GetComponent<AudioSource>();
        

        Line l;
        l.actor = "skipper";
        l.verse = "Im boutaa CUUUUUUUUUUUm i about to BLOOOOOOOOOOOOOW";
        l.lineAudio = null;

        Debug.Log("Voice manager start");
        AudioClip testClip = await getVoice(l);


        Debug.Log("Playing Audio Clip");
        audioSource.clip = testClip;
        audioSource.Play();
        */
    }

    

    // Update is called once per frame
    void Update()
    {

    }

    //public async Task<List<Line>> getVoices(List<Line> lines)
    //{

    //    int completedLines = 0;
    //    completedLinesText.text = String.Format("{0}/{1}", completedLines, lines.Count);
    //    for (int i = 0; i < lines.Count; i++)
    //    {

    //        Line line = lines[i];

    //        // Modify the struct
    //        if (actorsCode.ContainsKey(line.actor.ToLower()))
    //        {
    //            line.lineAudio = await getVoice(line);
    //        }
    //        else
    //        {
    //            line.lineAudio = null;
    //        }


    //        // Assign the modified struct back to the list
    //        lines[i] = line;
    //        completedLines++;
    //        completedLinesText.text = String.Format("{0}/{1}", completedLines, lines.Count);
    //    }

    //    return lines;
    //}

    public async Task<List<Line>> getVoices(List<Line> lines)
    {
        int completedLines = 0;
        completedLinesText.text = String.Format("{0}/{1}", completedLines, lines.Count);

        // List to store all the tasks that are running concurrently
        List<Task> tasks = new List<Task>();

        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];

            // Task to modify the struct and download the voice asynchronously
            Task downloadTask = Task.Run(async () =>
            {
                if (actorsCode.ContainsKey(line.actor.ToLower()))
                {
                    Debug.Log("Voice" + i + " started downloading");
                    line.lineAudio = await getVoice(line);  // Fetch the voice
                    Debug.Log("Voice" + i + " downloaded successfuly");
                }
                else
                {
                    line.lineAudio = null;
                }

                // Update the list and the completed counter after the download finishes
                lines[i] = line;
                completedLines++;
                //completedLinesText.text = String.Format("{0}/{1}", completedLines, lines.Count);
            });

            // Add the task to the list so we can await all tasks at the end
            tasks.Add(downloadTask);

            // Wait for 5 seconds before starting the next download
            if (i < lines.Count - 1)  // Ensure no delay after the last download
            {
                await Task.Delay(5000);
            }
        }

        // Await all the download tasks to complete
        await Task.WhenAll(tasks);

        return lines;
    }

    private async Task<AudioClip> getVoice(Line line)
    {
        //Generate uuid for the first request
        Guid uuid = Guid.NewGuid();

        Debug.Log("current line actor: " + line.actor.ToLower());
        PostRequestData requestData = new PostRequestData
        {
            uuid_idempotency_token = uuid.ToString(),
            tts_model_token = actorsCode[line.actor.ToLower()],
            inference_text = line.verse
        };

        PostRequestResult postData;

        postData = await getJobToken(requestData);


        //I WANT TO USE IT HERE
        Debug.Log(postData.inference_job_token);

        GetRequestResult getData;

        getData = await getJobUrl(postData.inference_job_token);
        Debug.Log(getData.state.maybe_public_bucket_wav_audio_path);

        AudioClip ac = await getAudioClip(getData.state.maybe_public_bucket_wav_audio_path);

        return ac;
    }

    private Task<AudioClip> getAudioClip(string audioUrl)
    {
        var tsc = new TaskCompletionSource<AudioClip>();
        StartCoroutine(DownloadAudio(tsc, audioUrl));
        return tsc.Task;
    }



    private IEnumerator DownloadAudio(TaskCompletionSource<AudioClip> tsc, string url)
    {
        // Create a UnityWebRequest with the DownloadHandlerAudioClip
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioUrl + url, AudioType.WAV))
        {
            // Send the request and wait for it to complete
            yield return uwr.SendWebRequest();

            // Check for errors
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error downloading audio clip: {uwr.error}");
                tsc.SetResult(null);
                yield break;
            }
            else
            {
                // Get the downloaded audio clip
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);

                // Assign the audio clip to the AudioSource and play it
                tsc.SetResult(clip);
                yield break;
            }
        }
    }

    private Task<GetRequestResult> getJobUrl(string jobToken)
    {
        var tsc = new TaskCompletionSource<GetRequestResult>();
        StartCoroutine(getJobUrlCoroutine(tsc, jobToken));
        return tsc.Task;


    }

    private IEnumerator getJobUrlCoroutine(TaskCompletionSource<GetRequestResult> tsc, string jobToken)
    {


        string status = "";

        do
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(urlJobStatus + jobToken))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Request Error: " + uwr.error);
                    yield break;
                }
                else
                {
                    string json = uwr.downloadHandler.text;
                    Debug.Log("Result json: " + json);
                    GetRequestResult result = JsonUtility.FromJson<GetRequestResult>(json);

                    status = result.state.status;

                    if (result.success == true && result.state.status == "complete_success")
                    {
                        
                        tsc.SetResult(result);
                        yield break;
                    }else if (result.success == false || result.state.status == "dead" || result.state.status == "complete_failure")
                    {
                        tsc.SetResult(null);
                        yield break;
                    }



                    
                   

                    if (result.state.status == "pending" || result.state.status == "attempt_failed" || result.state.status == "started")
                    {
                        Debug.Log("PENDING Waiting for another 2 seconds");
                        yield return new WaitForSeconds(5.0f);
                    }

                }

            }
        } while (status != "dead" && status != "complete_sucess" && status != "complete_failure");





    }



    private Task<PostRequestResult> getJobToken(PostRequestData requestData)
    {
        var tsc = new TaskCompletionSource<PostRequestResult>();
        StartCoroutine(getJobTokenCoroutine(tsc, requestData));
        return tsc.Task;

       
    }

    private IEnumerator getJobTokenCoroutine(TaskCompletionSource<PostRequestResult> tsc, PostRequestData requestData)
    {
        yield return new WaitForSeconds(5.0f);
        string requestBody = JsonUtility.ToJson(requestData);
        Debug.Log("Request Body: " + requestBody);
        using (UnityWebRequest uwr = UnityWebRequest.Post(urlJobToken, requestBody,"application/json"))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Request Error: " + uwr.error);
                yield break;
            }
            else
            {
                string json = uwr.downloadHandler.text;
                Debug.Log("Result json: " + json);
                PostRequestResult result = JsonUtility.FromJson<PostRequestResult>(json);

                if(result.success == true)
                {
                    tsc.SetResult(result);
                    yield break;
                }
                else
                {
                    tsc.SetResult(null);
                    yield break;
                }
                
            }
        }
        
       
        
    }

    
    [System.Serializable]
    public class PostRequestData
    {
        public string uuid_idempotency_token;
        public string tts_model_token;
        public string inference_text;
    }

    [System.Serializable]
    public class PostRequestResult
    {
        public bool success;
        public string inference_job_token;
        //"inference_job_token_type": "generic"
    }

    [System.Serializable]
    public class GetRequestResult
    {
        public bool success = false;
        public StateData state = null;

        [Serializable]
        public class StateData
        {
            public string status = "";
            public string maybe_public_bucket_wav_audio_path = "";
        }
    }


}
