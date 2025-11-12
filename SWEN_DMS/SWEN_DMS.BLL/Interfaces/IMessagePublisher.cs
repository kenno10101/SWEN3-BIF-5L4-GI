using System.Threading.Tasks;

namespace SWEN_DMS.BLL.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T payload, string? routingKeyOverride = null);
}