using System;
using System.Collections.Generic;
using System.Text;

namespace Ivony.Web
{
  public interface ISimpleRouteProvider
  {

    void BuildSimpleRoutes( ISimpleRouteBuilder builder );

  }
}
