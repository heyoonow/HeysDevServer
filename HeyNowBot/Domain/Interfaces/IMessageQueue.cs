namespace HeyNowBot.Domain.Interfaces
{
    public interface IMessageQueue
    {
        void Enqueue(string message);
    }
}
