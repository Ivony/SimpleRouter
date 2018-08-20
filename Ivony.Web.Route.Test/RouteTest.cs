using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http.Internal;

namespace Ivony.Web.Route.Test
{
  [TestClass]
  public class RouteTest
  {



    public SimpleRouteTable RouteTable { get; private set; }


    public RouteTest()
    {

      RouteTable = new SimpleRouteTable( "Default" );

    }


    [TestMethod]
    public void Default()
    {

      RouteTable.AddRule( "Default", "", new Dictionary<string, string> { ["Test"] = "Default" } );
      var data = RouteTable.GetRouteData( "GET", "~/", new QueryCollection() );

      Assert.AreEqual( data.Values["Test"], "Default" );

    }


  }
}
