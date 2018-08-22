﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using static Impala.Generated;
using static Impala.Errors;
using static Impala.Utilities;

namespace Impala
{
    public class ParBLX : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ParCurveCP class.
        /// </summary>
        public ParBLX()
          : base("ParBrepLineIntersect", "ParBLX",
              "Test whether a line and brep intersect.",
              "Impala", "Physical")
        {
            var error = new Error<(GH_Brep, GH_Line)>(NullCheck, NullHandle, this);
            CheckError = new ErrorChecker<(GH_Brep, GH_Line)>(error);
        }

        public ErrorChecker<(GH_Brep, GH_Line)> CheckError;
        static Func<(GH_Brep, GH_Line), bool> NullCheck = a => (a.Item1 != null && a.Item2 != null);


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep to test intersection", GH_ParamAccess.tree);
            pManager.AddLineParameter("Line", "L", "Line to intersect with", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Intersection", "I", "Result of intersection", GH_ParamAccess.tree);
            pManager.AddPointParameter("Points", "P", "Intersection points", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curves", "C", "Intersection overlap curves", GH_ParamAccess.tree);  
        }

        public static (GH_Curve[],GH_Point[],GH_Boolean[]) BLX(GH_Brep b, GH_Line l)
        {
            Brep brep = b.Value;
            Line line = l.Value;
            var failResult = (new GH_Curve[0], new GH_Point[0], Array(new GH_Boolean(false)));
            var bbox = brep.GetBoundingBox(false);
            if (!bbox.IsValid) return failResult;
            var ixline = Param_Line.LineThroughBox(line, bbox);
            if (!ixline.IsValid) return failResult;

            var ix = Intersection.CurveBrep(new LineCurve(ixline), brep, DocumentTolerance(), out Curve[] overlaps, out Point3d[] ixp);
            if (!ix) return failResult;

            var ghOlap = overlaps.Select(c => new GH_Curve(c)).ToArray();
            var ghPts = ixp.Select(pt => new GH_Point(pt)).ToArray();
            return (ghOlap, ghPts, Array(new GH_Boolean(true)));
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!DA.GetDataTree(0, out GH_Structure<GH_Brep> brepTree)) return;
            if (!DA.GetDataTree(1, out GH_Structure<GH_Line> lineTree)) return;

            var (cx,ptx,ix) = ZipGraft2x3(brepTree, lineTree, BLX, CheckError);

            DA.SetDataTree(0, ix);
            DA.SetDataTree(1, ptx);
            DA.SetDataTree(2, cx);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4F8227F1-DDB8-4FD5-B9D7-205AC212B616"); }
        }
    }
}