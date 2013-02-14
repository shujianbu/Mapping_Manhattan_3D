/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Helpers;

namespace DataVirtulization
{
    /// <summary>
    /// A pyramid geometry primitive constructed with CustomMesh
    /// </summary>
    public class Pyramid : PrimitiveModel
    {
        /// <summary>
        /// Create a pyramid with the given dimensions
        /// </summary>
        /// <param name="xDim"></param>
        /// <param name="yDim"></param>
        /// <param name="zDim"></param>
        public Pyramid(float xDim, float yDim, float zDim) :
            base(CreateShape(xDim, yDim, zDim))
        {
            customShapeParameters = xDim + ", " + yDim + ", " + zDim;
        }

        private static CustomMesh CreateShape(float xDim, float yDim, float zDim)
        {
            // Create a primitive mesh for creating our custom pyramid shape
            CustomMesh pyramid = new CustomMesh();

            // Even though we really need 5 vertices to create a pyramid shape, since
            // we want to assign different normals for points with same position to have
            // more accurate lighting effect, we will use 16 vertices.
            VertexPositionNormal[] verts = new VertexPositionNormal[16];

            Vector3 vBase0 = new Vector3(-xDim / 2, 0, zDim / 2);
            Vector3 vBase1 = new Vector3(xDim / 2, 0, zDim / 2);
            Vector3 vBase2 = new Vector3(xDim / 2, 0, -zDim / 2);
            Vector3 vBase3 = new Vector3(-xDim / 2, 0, -zDim / 2);
            Vector3 vTop = new Vector3(0, yDim, 0);

            verts[0].Position = vTop;
            verts[1].Position = vBase1;
            verts[2].Position = vBase0;

            verts[3].Position = vTop;
            verts[4].Position = vBase2;
            verts[5].Position = vBase1;

            verts[6].Position = vTop;
            verts[7].Position = vBase3;
            verts[8].Position = vBase2;

            verts[9].Position = vTop;
            verts[10].Position = vBase0;
            verts[11].Position = vBase3;

            verts[12].Position = vBase0;
            verts[13].Position = vBase1;
            verts[14].Position = vBase2;
            verts[15].Position = vBase3;

            verts[0].Normal = verts[1].Normal = verts[2].Normal = CalcNormal(vTop, vBase1, vBase0);
            verts[3].Normal = verts[4].Normal = verts[5].Normal = CalcNormal(vTop, vBase2, vBase1);
            verts[6].Normal = verts[7].Normal = verts[8].Normal = CalcNormal(vTop, vBase3, vBase2);
            verts[9].Normal = verts[10].Normal = verts[11].Normal = CalcNormal(vTop, vBase0, vBase3);
            verts[12].Normal = verts[13].Normal = verts[14].Normal = verts[15].Normal = CalcNormal(vBase0,
                vBase1, vBase3);

            pyramid.VertexBuffer = new VertexBuffer(State.Device,
                typeof(VertexPositionNormal), 16, BufferUsage.None);
            pyramid.VertexDeclaration = VertexPositionNormal.VertexDeclaration;
            pyramid.VertexBuffer.SetData(verts);
            pyramid.NumberOfVertices = 16;

            short[] indices = new short[18];

            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;

            indices[3] = 3;
            indices[4] = 4;
            indices[5] = 5;

            indices[6] = 6;
            indices[7] = 7;
            indices[8] = 8;

            indices[9] = 9;
            indices[10] = 10;
            indices[11] = 11;

            indices[12] = 12;
            indices[13] = 13;
            indices[14] = 15;

            indices[15] = 13;
            indices[16] = 14;
            indices[17] = 15;

            pyramid.IndexBuffer = new IndexBuffer(State.Device, typeof(short), 18,
                BufferUsage.WriteOnly);
            pyramid.IndexBuffer.SetData(indices);

            pyramid.PrimitiveType = PrimitiveType.TriangleList;
            pyramid.NumberOfPrimitives = 6;

            return pyramid;
        }

        /// <summary>
        /// Calculate the normal of two vectors v0->v1 and v0->v2 using right hand rule
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private static Vector3 CalcNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 v0_1 = v1 - v0;
            Vector3 v0_2 = v2 - v0;

            Vector3 normal = Vector3.Cross(v0_2, v0_1);
            normal.Normalize();

            return normal;
        }
    }
}
