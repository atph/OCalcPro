using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPL_Model_Wrapper;
namespace TestPoleMaker
{
    class Example
    {
        public PPL_Model_Wrapper.PPLX BuildExample()
        {
            //create base scene
            PPL_Model_Wrapper.PPLX pplx = new PPL_Model_Wrapper.PPLX();
            pplx.Scene = new PPL_Model_Wrapper.Scene(true);

            //add pole and loadcase
            PPL_Model_Wrapper.WoodPole pole = new PPL_Model_Wrapper.WoodPole(true);
            pole.LengthInInches = 40 * 12;
            pole.Children.Add(new PPL_Model_Wrapper.LoadCase(true));
            pplx.Scene.AddChild(pole);

            //Add insulator to the top of the pole
            Insulator ins = new Insulator(true);
            ins.CoordinateZ = pole.LengthInInches;
            ins.Type = Insulator.Type_val.Pin;
            pole.AddChild(ins);

            //add a span to the insulator
            Span frontSpan = new Span(true);
            frontSpan.SpanDistanceInInches = 150 * 12;
            frontSpan.SpanType = Span.SpanType_val.Primary;
            frontSpan.CoordinateA = 0; //radians
            ins.AddChild(frontSpan);

            Span backSpan = new Span(true);
            backSpan.SpanDistanceInInches = 150 * 12;
            backSpan.SpanType = Span.SpanType_val.Primary;
            backSpan.CoordinateA = Math.PI;  //radians
            ins.AddChild(backSpan);

            return pplx;
        }
    }
}
