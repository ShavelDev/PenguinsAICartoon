using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

public class GPTManager : MonoBehaviour
{
    public static string currentActor = "";
    public string actorSpeaking = "";

    public AudioSource audioSource;
    public VoicesManager vm;
    
   

    public TMP_Text textField;
    public TMP_InputField inputField;
    public Button okButton;

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    void Start()
    {
        api = new OpenAIAPI("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");
        StartConversation();
        okButton.onClick.AddListener(() => GetResponce());
    }

    void StartConversation()
    {
        messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRole.System, getSystemPrompt())
        };

        inputField.text = "";

    }

    private async void GetResponce()
    {
        if (inputField.text.Length < 10)
        {
            return;
        }

        okButton.enabled = false;

        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputField.text;

        /*
        if (userMessage.Content.Length > 100)
        {
            userMessage.Content = userMessage.Content.Substring(0, 100); nigger
        }
        */
        messages.Add(userMessage);

        //textField.text = string.Format("You: {0}", userMessage.Content);

        inputField.text = "";

        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 1,
            MaxTokens = 800,
            Messages = messages
        });

        ChatMessage responseMessage = new ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        Debug.Log(chatResult.Choices[0].Message.Role);
        responseMessage.Content = chatResult.Choices[0].Message.Content;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));


        //I think this is to make the chatgpt remember the context of previous question
        //messages.Add(responseMessage);

        //Instead of this, go through every character and try to find actors
        //textField.text = string.Format("You: {0}\n\nGuard: {1}", userMessage.Content, responseMessage.Content);

        string response = responseMessage.Content;
        response += "[";

        List<Line> lineList = new List<Line>();
        string currentName = "";
        string currentVerse = "";
        bool isActorName = false;

        foreach (char c in response)
        {
            


            if (c == '[')
            {
                Debug.Log("CurrentName: " + currentName);
                lineList.Add(new Line(currentName, currentVerse));
                currentName = "";
                currentVerse = "";
                isActorName = true;
            }
            else if (c == ']')
            {
                isActorName = false;

            }


            if (isActorName && c != '[' && c != ']')
            {
                currentName += c;
            }
            else if(isActorName == false && c != '[' && c != ']')
            {
                currentVerse += c;
            }
        }

        //here get the voices for each line
        lineList = await vm.getVoices(lineList);



        //starting the scene
        StartCoroutine(waiter(lineList));
        Debug.Log("Code after coroutine");

        List<ChatMessage> messagesToRemove = new List<ChatMessage>();
        
        foreach (ChatMessage m in messages)
        {


            try
            {
                Debug.Log(m.Content);
            }
            catch(Exception e)
            {
                Debug.Log("ERROR CAUGHT: " + e.Message);
            }
            if (m.Role != ChatMessageRole.System)
            {
                //Debug.Log("Content: " + m.Content + " Role: " + m.Role);
                messagesToRemove.Add(m);
            }
        }


        Debug.Log("messages before: " + messages.Count);
        foreach (ChatMessage m in messagesToRemove)
        {
            
            messages.Remove(m);
        }
        Debug.Log("messages after: " + messages.Count);


    }


    IEnumerator waiter(List<Line> lineList)
    {


        
        foreach (Line l in lineList)
        {
            GPTManager.currentActor = l.actor.ToLower();
            Debug.Log("current Actor: " + GPTManager.currentActor);
            textField.text = l.verse;

            //here change the audio to the line audio clip
            audioSource.clip = l.lineAudio;
            audioSource.Play();


            
            yield return new WaitForSeconds(audioSource.clip? (audioSource.clip.length + 1.0f) : 0.0f);

            //here change the waiting time to the number of seconds of the audio clip
            //if (l.verse.Length > 130)
            //{
            //    yield return new WaitForSeconds(8);
            //}
            //else
            //{
            //    yield return new WaitForSeconds(5);
            //}
            

        }



        okButton.enabled = true;

    }



    private string getSystemPrompt()
    {
        return @"You are in charge of writing a Penguins of Madagascar episode, you can only give very basic stage directions,
here is the list of stage directions that you can use:
The whole script takes place in the zoo enclosure for penguins.
The characters you are allowed to use include Skipper, Kowalski, Private.
You CANNOT use all of the characters.
Make sure you are not including any character than the three mentioned.
Make sure to add some funny scifi jokes if Kowalski is speaking.

Please structure the script by Stating the Person that is speaking then : separate speakers by new lines.
Include the name of the person that is speaking in square brackets.
Its important to use light profanity like frick and crap.
Do NOT add any other directions to define tone or anything, only dialog.
You MUST NOT include anything except the names of characters in the square brackets like this: []
In the user prompt will be provided the topic that you need to write a script about.

Remember to format the script like the two examples below:

Example 1:

[Skipper]
Alright, team, gather 'round. We've got a top-secret mission on our flippers.
[Kowalski]
Fantastic! Is it finally time for my intergalactic wormhole experiment?
[Private]
Uh, intergalactic what now?
[Skipper]
Easy there, Kowalski. We've got a situation in sector 7-G. It seems Mort's been playing with the zoo's remote control again.
[Kowalski]
Ah, classic Mort. What's he gotten himself into this time?
[Private]
Is it another Operation: Escaping Butterflies?
[Skipper]
No, Private.This time, he's accidentally opened a portal to an alternate dimension.
[Kowalski]
You're kidding!
[Skipper]
Wish I was, but we need to find him before anything else comes through.
[Private]
        And what if we encounter, you know, alternate versions of ourselves?
        [Kowalski]
Well, Private, I suppose we'll have to be extra charming.
[Skipper]
Just stick together, team.Let's move out!

Example 2:

[Kowalski]
        Alright team, gather 'round. We've got a little hiccup in the interdimensional portal generator.
[Private]
        Um, what's happening, Kowalski?
        [Kowalski]
It appears that the interdimensional portal generator is acting up again.I suspect a graviton destabilization.
[Private]
Graviton... huh?
[Kowalski]
Don't worry about it, Private. What's important is that if we don't fix it, we might accidentally open a portal to a dimension full of giant rubber duckies. Trust me, that's not as quack - tastic as it sounds.
  [Private]
Oh my!That does sound serious.
[Skipper]
Alright, let's get to work. Private, grab the duct tape. Kowalski, recalibrate the quantum wrench.
[Private]
Got the duct tape, Skipper!
[Kowalski]
Recalibrating, Skipper.Oh, and by the way, did you hear about the penguin who tried to fix his 2006 Honda Civic with duct tape? It was a real quacker of an idea!
[Skipper]
Frick, Kowalski.Let's stick to interdimensional portals for now.
[Private]
Um, guys? I think I might've stuck the duct tape to itself.
[Kowalski]
Frick, Private.Alright, hand it over.I'll try to salvage it.
[Skipper]
Time's ticking, team. We need that portal sealed ASAP.
[Kowalski]
Almost there... and... success! Portal sealed and stable.
[Private]
Phew, crisis averted.
[Skipper]
Good work, boys.Remember, we penguins might not be rocket scientists, but we always find a way to wing it.
[Kowalski]
Indeed, Skipper.And speaking of rocket science, did you know that in Dimension Alpha-12...
[Skipper]
Save it for the debrief, Kowalski.Let's grab some fish and call it a day. And Kowalski, no more car jokes, got it?";
    }


}
