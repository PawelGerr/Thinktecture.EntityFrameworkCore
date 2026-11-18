using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore;

internal sealed class ThrowingDesignTimeModel : IDesignTimeModel
{
   public IModel Model => throw new InvalidOperationException("The design-time model must not be requested.");
}
