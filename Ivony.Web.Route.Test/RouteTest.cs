using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Ivony.Web.Route.Test
{
  [TestClass]
  public class RouteTest
  {



    public SimpleRouteTable RouteTable { get; private set; }


    public RouteTest()
    {


      var services = new ServiceCollection();
      services.AddLogging();

      RouteTable = new SimpleRouteTable( "Default", services.BuildServiceProvider() );

    }


    [TestMethod]
    public void Default()
    {

      RouteTable.MapRoute( "Default", "", new Dictionary<string, string> { ["Test"] = "Default" } );
      var data = RouteTable.GetRouteData( "GET", "~/", new QueryCollection() );

      Assert.AreEqual( data.Values["Test"], "Default" );

    }

    [TestMethod]
    public void Conflict1()
    {
      
      RouteTable.MapRoute( "A", "A/{action}" );
      RouteTable.MapRoute( "Nested", "Nested/A/{action}" );

    }


  }
}
