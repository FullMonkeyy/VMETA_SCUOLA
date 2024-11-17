using OllamaSharp.Models.Chat;
using OllamaSharp.Streamer;
using OllamaSharp;

namespace VMETA_1.Classes
{
    public class VanessaCore
    {
        OllamaApiClient ollama;
        List<Message> _messages;
        ChatRequest chatRequest;
        IResponseStreamer<ChatResponseStream?> Streamer;
        CancellationTokenSource cts;


        public VanessaCore(IResponseStreamer<ChatResponseStream?> streamer)
        {
            //DEV: http://localhost:11434
            //RELEASE: http://192.168.1.52:11434

            //FAI ATTENZIONE AL MODELLO SCRITTO NELLA REQUEST
            ollama = new OllamaApiClient(new Uri(" http://localhost:11434"));
           
            _messages = new List<Message>();
            Streamer = streamer;
            cts = new CancellationTokenSource();

            List<Messaggio> messaggini = GestioneFile.ReadXMLConversation(GestioneFile.path3);
            foreach (Messaggio messaggio in messaggini)
            {

                if (messaggio.Role.Equals("User"))
                {
                    _messages.Add(new Message(ChatRole.User, messaggio.Message));
                }
                else
                {
                    _messages.Add(new Message(ChatRole.Assistant, messaggio.Message));
                }


            }
     
            foreach (Messaggio messaggio in messaggini)
            {

                if (messaggio.Role.Equals("User"))
                {
                    _messages.Add(new Message(ChatRole.User, messaggio.Message));
                }
                else
                {
                    _messages.Add(new Message(ChatRole.Assistant, messaggio.Message));
                }


            }

        }

        public async Task<bool> TalkWithVanessa(string prompt)
        {
            
            chatRequest = new ChatRequest
            {
                Messages = _messages,
                Model = "llama3",
                Stream = true
            };
            try
            {
                _messages = (await ollama.SendChat(chatRequest, Streamer, cts.Token)).ToList();
                return true;
            }
            catch (Exception ex) {

                Console.WriteLine("C'è stata un errore di comunicazione http con ollama");
                return false;
            
            }
        }

        public async Task<bool> TalkWithVanessa(string prompt,bool isContesutalized)
        {
            if(isContesutalized)
            _messages.Add(new Message(ChatRole.User, prompt));
            chatRequest = new ChatRequest
            {
                Messages = _messages,
                Model = "llama3",
                Stream = true
            };
            try
            {
                _messages = (await ollama.SendChat(chatRequest, Streamer, cts.Token)).ToList();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine("C'è stata un errore di comunicazione http con ollama, la porta non ha risposto correttamente oppure non è stata trovata");
                return false;

            }
        }


    }
}
