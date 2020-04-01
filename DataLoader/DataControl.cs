using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Xml;
using System.Drawing;
using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics.Scene.SpecialObjects;
using anim = WiccanRede.Graphics.Scene.SpecialObjects.AnimatedObject;

namespace WiccanRede
{
    public class DataControl
    {
        
        private Device dev;
        private List<Entity> entities;
        private EffectPool shaderPool;
        private AnimationRootFrame rootFrame;
        private int progress;
        private int entityCount;
        private List<MeshInformation> meshInf;

        public DataControl(Device device, EffectPool pool)
        {
            meshInf = new List<MeshInformation>();
            shaderPool = pool;
            dev = device;
            entities = new List<Entity>();
        }
        /// <summary>
        /// Pridáni objektu typu Entity do zasobníku , který bude uložen pomocí metody SaveXML()
        /// </summary>
        /// <param name="newEntity">Entita kterou chceme pridat</param>
        public void AddEntity(Entity newEntity)
        {
            entities.Add(newEntity);
        }
        /// <summary>
        /// Ulozeni souboru xml , ukládají se všechny objekty , které byli přidány metodou AddEntity
        /// </summary>
        /// <param name="xmlFileName">nazev Xml souboru</param>
        public void SaveXML(string xmlFileName)
        {
            //Dom struktura pro dokument
            XmlDocument XmlFile = new XmlDocument();

            //vytvoreni Xml Souboru
            XmlFile.LoadXml("<?xml version='1.0' ?><root></root>");

            //Tag s objektama
            XmlElement objects = XmlFile.CreateElement("Objekts");

            for (int i = 0; i < entities.Count; i++)
            {

                XmlElement main = XmlFile.CreateElement("Entity");
                main.SetAttribute("type", entities[i].Type);

                List<Parametr> parametrs = entities[i].GetParametrs();

                //ukladani jednotlivých parametru
                for (int n = 0; n < parametrs.Count; n++)
                {
                    XmlElement param = XmlFile.CreateElement(parametrs[n].name);
                    param.SetAttribute("type", parametrs[n].type);

                    //ukladani stringu pouze ulozeni
                    if (parametrs[n].value is string)
                    {
                        string conv = (string)parametrs[n].value;
                        param.SetAttribute("string", conv);
                    }
                    // matice se prevadi pomoci ConvertMatrixToString() na string
                    else if (parametrs[n].value is Matrix)
                    {
                        Matrix conv = (Matrix)parametrs[n].value;
                        param.SetAttribute("matrix", ConvertMatrixToString(conv));
                    }

                    // barvz se prevadi pomoci ConvertColorToString() na string
                    else if (parametrs[n].value is Color)
                    {
                        Color conv = (Color)parametrs[n].value;
                        param.SetAttribute("color", ConvertColorToString(conv));
                    }
                    //bool pomoci metody ToString
                    else if (parametrs[n].value is bool)
                    {
                        param.SetAttribute("bool", parametrs[n].value.ToString());
                    }
                    // integer pomoci ToString
                    else if (parametrs[n].value is int)
                    {
                        param.SetAttribute("int", parametrs[n].value.ToString());
                    }
                    //float pomoci ToString
                    else if (parametrs[n].value is float)
                    {
                        param.SetAttribute("float", parametrs[n].value.ToString());
                    }
                    //Pro pole se pro kazdou hodnotu v poli vytvori vlastni tag a kazda hodnota se 
                    // prevadi pomoci ToString
                    else if (parametrs[n].value is Array)
                    {

                        string[] conv = (string[])parametrs[n].value;
                        param.SetAttribute("lenght", conv.Length.ToString());
                        for (int m = 0; m < conv.Length; m++)
                        {
                            XmlElement text = XmlFile.CreateElement("tag");
                            text.SetAttribute("index", m.ToString());
                            text.SetAttribute("data", conv[m]);
                            param.AppendChild(text);
                        }

                    }
                    // Pokud se jedna o neznamy datovy typ Chyba
                    else
                    {
                        param.SetAttribute("CHYBA ", parametrs[n].value.ToString());
                    }


                    main.AppendChild(param);
                }
                

                objects.AppendChild(main);
            }

            XmlFile.DocumentElement.AppendChild(objects);
            XmlFile.Save(xmlFileName);

        }
        /// <summary>
        /// nacteni objektu (Entit) z Xml souboru
        /// </summary>
        /// <param name="xmlFileName">nazev Xml souboru</param>
        /// <returns>null - pokud se nepovedlo nacist soubor
        ///          List Entit - pokud nacteni probehlo uspesne </returns>
        public List<Entity> LoadXML(string xmlFileName)
        {
            XmlDocument XmlFile = new XmlDocument();
            if (!File.Exists(xmlFileName)) return null;
            XmlFile.Load(xmlFileName);

            XmlNodeList objects = XmlFile.DocumentElement.GetElementsByTagName("Objekts");
            entityCount = objects[0].ChildNodes.Count;
            for (progress = 0; progress < objects[0].ChildNodes.Count; progress++)
            {
                bool loadSpecialTexture = false;

                List<string> specialTextureType = new List<string>();
                List<string> specialTextureName = new List<string>(); 

                XmlNodeList parametrs = objects[0].ChildNodes[progress].ChildNodes;

                Entity loaded = new Entity(objects[0].ChildNodes[progress].InnerText);
                loaded.Type = objects[0].ChildNodes[progress].Attributes[0].InnerText;

                
                for (int n = 0; n < parametrs.Count; n++)
                {
                    Parametr actual = new Parametr();

                    actual.type = parametrs[n].Attributes[0].InnerText;
                    actual.name = parametrs[n].Name;

                    string typeOfValue = parametrs[n].Attributes[1].Name;

                    if (typeOfValue.CompareTo("string") == 0)
                    {
                        if (actual.type.CompareTo("Texture") == 0)
                        {
                            loadSpecialTexture = true;
                            specialTextureType.Add(parametrs[n].Attributes[1].InnerText);
                            specialTextureName.Add(parametrs[n].Name);

                        }
                        else if (actual.type.CompareTo("Bitmap") == 0)
                        {
                            loaded = LoadBitMap(loaded, parametrs[n].Attributes[1].InnerText, actual.name);

                        }
                        else if (actual.type.CompareTo("Shader") == 0)
                        {
                            loaded = LoadShaderFromFile(loaded, parametrs[n].Attributes[1].InnerText, actual.name);

                        }
                        else if (actual.type.CompareTo("Animation") == 0)
                        {
                            loaded = LoadAnimatedObject(loaded, parametrs[n].Attributes[1].InnerText, actual.name);

                        }
                        else
                        {
                            actual.value = parametrs[n].Attributes[1].InnerText;
                            loaded.AddParametr(actual);
                        }
                    }
                    else if (typeOfValue.CompareTo("matrix") == 0)
                    {
                        actual.value = ConvertStringToMatrix(parametrs[n].Attributes[1].InnerText);
                        loaded.AddParametr(actual);
                    }
                    else if (typeOfValue.CompareTo("color") == 0)
                    {
                        actual.value = ConvertStringToColor(parametrs[n].Attributes[1].InnerText);
                        loaded.AddParametr(actual);
                    }
                    else if (typeOfValue.CompareTo("bool") == 0)
                    {
                        actual.value = ConvertStringToBool(parametrs[n].Attributes[1].InnerText);
                        loaded.AddParametr(actual);
                    }
                    else if (typeOfValue.CompareTo("int") == 0)
                    {
                        actual.value = int.Parse(parametrs[n].Attributes[1].InnerText);
                        loaded.AddParametr(actual);
                    }
                    else if (typeOfValue.CompareTo("float") == 0)
                    {
                        actual.value = float.Parse(parametrs[n].Attributes[1].InnerText);
                        loaded.AddParametr(actual);

                    }
                    else if (typeOfValue.CompareTo("lenght") == 0)
                    {
                        string[] urls = new string[int.Parse(parametrs[n].Attributes[1].InnerText)];

                        for (int m = 0; m < parametrs[n].ChildNodes.Count; m++)
                        {
                            urls[m] = parametrs[n].ChildNodes[m].Attributes[1].InnerText;
                        }
                        if (actual.type.CompareTo("meshX") == 0)
                        {
                            loaded = LoadMeshFromFile(loaded, urls, parametrs[n].Name);
                        }
                        else
                        {

                            loaded = LoadTexturesFromFile(loaded, urls, parametrs[n].Attributes[0].InnerText, parametrs[n].Name);
                        }
                    }
                    else
                    {
                        throw new Exception("Chyba pri nacitani parametru z xml neznamy typ" + typeOfValue);
                    }

                }
                if (loadSpecialTexture)
                    LoadSpecialTextures(loaded, specialTextureType, specialTextureName);

                if (loaded["SpecialTextureUrl"] != null)
                {
                    loaded.RemoveParametr(loaded["SpecialTextureUrl"]);
                }
                entities.Add(loaded);

            }
            return entities;

        }
        /// <summary>
        /// prevedeni macice na string 
        /// </summary>
        /// <param name="converted">Matice ktera ma byt prevedena</param>
        /// <returns>string reprezentujici danou matici</returns>
        private string ConvertMatrixToString(Matrix converted)
        {
            string stringMatrix = "";

            stringMatrix += converted.M11 + " ";
            stringMatrix += converted.M12 + " ";
            stringMatrix += converted.M13 + " ";
            stringMatrix += converted.M14 + " ";
            stringMatrix += converted.M21 + " ";
            stringMatrix += converted.M22 + " ";
            stringMatrix += converted.M23 + " ";
            stringMatrix += converted.M24 + " ";
            stringMatrix += converted.M31 + " ";
            stringMatrix += converted.M32 + " ";
            stringMatrix += converted.M33 + " ";
            stringMatrix += converted.M34 + " ";
            stringMatrix += converted.M41 + " ";
            stringMatrix += converted.M42 + " ";
            stringMatrix += converted.M43 + " ";
            stringMatrix += converted.M44;

            return stringMatrix;
        }
        /// <summary>
        /// prevedeni stringu na matici
        /// </summary>
        /// <param name="converted">String reprezentujici danou matici</param>
        /// <returns>Matici prevedenou ze stringu</returns>
        private Matrix ConvertStringToMatrix(String converted)
        {
            Matrix conv;
            string[] numbers = converted.Split(' ');
            if (numbers.Length != 16)
                throw new Exception("nemohu prevest Matici ze stringu");

            conv.M11 = float.Parse(numbers[0]);
            conv.M12 = float.Parse(numbers[1]);
            conv.M13 = float.Parse(numbers[2]);
            conv.M14 = float.Parse(numbers[3]);
            conv.M21 = float.Parse(numbers[4]);
            conv.M22 = float.Parse(numbers[5]);
            conv.M23 = float.Parse(numbers[6]);
            conv.M24 = float.Parse(numbers[7]);
            conv.M31 = float.Parse(numbers[8]);
            conv.M32 = float.Parse(numbers[9]);
            conv.M33 = float.Parse(numbers[10]);
            conv.M34 = float.Parse(numbers[11]);
            conv.M41 = float.Parse(numbers[12]);
            conv.M42 = float.Parse(numbers[13]);
            conv.M43 = float.Parse(numbers[14]);
            conv.M44 = float.Parse(numbers[15]);
            return conv;
        }
        /// <summary>
        /// prevedeni stringu na bool  
        /// </summary>
        /// <param name="converted">string s hodnotou "true" nebo "false"</param>
        /// <returns>bool</returns>
        private bool ConvertStringToBool(String converted)
        {
            if (converted.CompareTo("true") == 0)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// prevedeni barvy na string
        /// </summary>
        /// <param name="converted">barva</param>
        /// <returns>string reprezentujici danou barvu</returns>
        private string ConvertColorToString(Color converted)
        {
            string stringColor = "";
            if (converted == Color.Empty)
                return "noColor";
            stringColor += converted.R.ToString() + " ";
            stringColor += converted.G.ToString() + " ";
            stringColor += converted.B.ToString() + " ";
            stringColor += converted.A.ToString();
            return stringColor;
        }
        /// <summary>
        /// prevedeni stringu na barvu
        /// </summary>
        /// <param name="converted">string reprezentujici danou barvu</param>
        /// <returns>barva</returns>
        private Color ConvertStringToColor(String converted)
        {
            Color conv;
            string[] colors;

            if (converted.CompareTo("noColor") == 0)
                return Color.Empty;

            colors = converted.Split(' ');
            if (colors.Length != 4)
                throw new Exception("nemohu prevest barvu ze stringu");
            conv = Color.FromArgb(int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2]), int.Parse(colors[3]));
            return conv;


        }
        /// <summary>
        /// nacteni textur ze souboru
        /// </summary>
        /// <param name="loaded">Entita do ktere bude pridan parametr s texturama </param>
        /// <param name="textureUrl">s url textur</param>
        /// <param name="type">Typ textur pro ukladani parametru</param>
        /// <param name="name">jmeno textur pro ukladani parametru</param>
        /// <returns>Entita s doplnenym parametrem - pole texturem</returns>
        private Entity LoadTexturesFromFile(Entity loaded, string[] textureUrl, string type, string name)
        {

            Texture[] textures = new Texture[textureUrl.Length];


            string pathname = Path.GetDirectoryName(textureUrl[0]) + @"\";

            for (int i = 0; i < textureUrl.Length; i++)
            {
                if (textureUrl[i] == null)
                    continue;

                string abspath = (pathname + textureUrl[i]);
                string path = textureUrl[i];

                bool createtexture = true;
                for (int t = 0; t < i; t++)
                {
                    if (textureUrl[t].CompareTo(textureUrl[i]) == 0)
                    {

                        textures[i] = textures[t];
                        createtexture = false;
                        break;
                    }
                }

                if (createtexture)
                {
                    if (File.Exists(path))
                    {
                        textures[i] = TextureLoader.FromFile(dev, path, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                    }
                    else if (File.Exists(abspath))
                    {
                        textures[i] = TextureLoader.FromFile(dev, abspath, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                    }
                }

            }
            if (textures.Length == 1)
            {
                loaded.AddParametr(new Parametr(type, name, textures[0]));
            }
            else
            {
                loaded.AddParametr(new Parametr(type, name, textures));
            }
            return loaded;


        }
        /// <summary>
        /// Nacteni meshe a textur a vytvoreni progressive meshe
        /// </summary>
        /// <param name="loaded">Entita do ktere chceme pridat mesh a texturz</param>
        /// <param name="data">pole obsahujuci nazev meshe a textur</param>
        /// <param name="name">nazev objektu</param>
        /// <returns>Entita s meshem a texturami</returns>
        private Entity LoadMeshFromFile(Entity loaded, string[] data, string name)
        {
            string[] textureUrl;
            
            string objectUrl = data[0];
            int p = FindMesh(objectUrl);
           
            if (p!= -1) {
                
                loaded.AddParametr(new Parametr("Objekt[]", data[1], meshInf[p].textures));
                loaded.AddParametr(new Parametr("Microsoft.DirectX.Direct3D.ProgressiveMesh", name, meshInf[p].pMesh));
                loaded.AddParametr(new Parametr("Objekt[]", "SpecialTextureUrl",meshInf[p].texturesUrl));
                return loaded;
            }
            if (data.Length < 2) throw new Exception("Chyba pri nacitani nespravny pocet parametru");

            Texture[] textures;
            ProgressiveMesh currentLoadedMesh;

            Matrix localTransformation = Matrix.Identity;

            GraphicsStream adjency = null;
            ExtendedMaterial[] mat = null;

            using (Mesh mesh = Mesh.FromFile(objectUrl, MeshFlags.Managed, dev, out adjency, out mat))
            {

                #region Texture Loading

                textures = new Texture[mat.Length];
                textureUrl = new string[mat.Length];

                string pathname = Path.GetDirectoryName(objectUrl) + @"\";

                for (int i = 0; i < mat.Length; i++)
                {

                    if (mat[i].TextureFilename == null)
                        continue;



                    string abspath = (pathname + mat[i].TextureFilename);
                    textureUrl[i] = abspath;
                    string path = mat[i].TextureFilename;

                    bool createtexture = true;
                    for (int t = 0; t < i; t++)
                    {
                        if (mat[t].TextureFilename == mat[i].TextureFilename)
                        {
                            textures[i] = textures[t];
                            createtexture = false;
                            break;
                        }
                    }

                    if (createtexture)
                    {
                        if (File.Exists(path))
                        {
                            textures[i] = TextureLoader.FromFile(dev, path, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                        }
                        else if (File.Exists(abspath))
                        {
                            textures[i] = TextureLoader.FromFile(dev, abspath, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                        }
                    }

                }

                loaded.AddParametr(new Parametr("Objekt[]", "SpecialTextureUrl", textureUrl));
                #endregion


                #region Mesh clean, optimize and compute normals/tangents

                int use32Bit = (int)(mesh.Options.Value & MeshFlags.Use32Bit);

                using (Mesh currentmesh = Mesh.Clean(CleanType.Simplification, mesh, adjency, adjency))
                {
                    WeldEpsilons epsilons = new WeldEpsilons();
                    currentmesh.WeldVertices(0, epsilons, adjency, adjency);
                    currentmesh.Validate(adjency);

                    Mesh newmesh = currentmesh.Optimize(MeshFlags.OptimizeStripeReorder | MeshFlags.OptimizeAttributeSort, adjency);
                    using (newmesh = currentmesh.Clone(MeshFlags.Managed | (MeshFlags)use32Bit, GeneralObject.GeneralVertex.vertexElements, dev))
                    {
                        newmesh.ComputeNormals();

                        ProgressiveMesh pm = new ProgressiveMesh(newmesh, adjency, null, 1, MeshFlags.SimplifyFace);

                        currentLoadedMesh = pm;
                        currentLoadedMesh.NumberFaces = currentLoadedMesh.MaxFaces;
                    }
                }
                #endregion

            }
            meshInf.Add(new MeshInformation(currentLoadedMesh, objectUrl, textures, textureUrl));

            loaded.AddParametr(new Parametr("Objekt[]", data[1], textures));
            loaded.AddParametr(new Parametr("Microsoft.DirectX.Direct3D.ProgressiveMesh", name, currentLoadedMesh));

           

            return loaded;

        }
        /// <summary>
        /// 
        /// Metoda vraci v procentech prubeh nacitani XML souboru
        /// </summary>
        /// <returns>procenta prubehu</returns>
        public int GetProgressInfo()
        {
            double percents = ((double)progress / (double)entityCount) * 100f;
            return (int)percents;
        }

        private int FindMesh(String path)
        {
            for (int i = 0; i < meshInf.Count; i++)
            {
                if (meshInf[i].path == path) return i;
            }
            return -1;
        }
        /// <summary>
        /// Nacteni specialnich textur
        /// </summary>
        /// <param name="loaded">Entita do ktere chceme nacist textury</param>
        /// <param name="types">pole s typama textur ktere chceme nacist</param>
        /// <param name="names">jmena textur</param>
        /// <returns>Entita s texturama</returns>
        private Entity LoadSpecialTextures(Entity loaded, List<string> types, List<string> names)
        {


            String[] textureUrl = (String[])loaded["SpecialTextureUrl"].value;
            Texture[] textures;
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i].CompareTo("normal") == 0)
                {
                    textures = loadNormalTextures(textureUrl);
                    loaded.AddParametr(new Parametr("Object[]", names[i], textures));
                }
                else if (types[i].CompareTo("color1") == 0)
                {
                    textures = loadColorTexture(textureUrl, "_color1");
                    loaded.AddParametr(new Parametr("Object[]", names[i], textures));
                }
                else if (types[i].CompareTo("color2") == 0)
                {
                    textures = loadColorTexture(textureUrl, "_color2");
                    loaded.AddParametr(new Parametr("Object[]", names[i], textures));
                }

            }
            return loaded;
        }
        /// <summary>
        /// Nacteni specialnich textur
        /// </summary>
        /// <param name="textureUrl">Pole s adresami k texturam</param>
        /// <param name="color">Nazev barvy</param>
        /// <returns>Pole specialnich textur</returns>
        private Texture[] loadColorTexture(string[] textureUrl, string color)
        {
            Texture[] colorTexture = new Texture[textureUrl.Length];
            string path;


            for (int i = 0; i < textureUrl.Length; i++)
            {
                bool createtexture = true;

                for (int t = 0; t < i; t++)
                {
                    if (textureUrl[t] == textureUrl[i])
                    {
                        colorTexture[i] = colorTexture[t];
                        createtexture = false;
                        break;
                    }
                }
                if (createtexture)
                {
                    int ins = textureUrl[i].LastIndexOf(".");
                    path = textureUrl[i].Insert(ins, color);

                    if (File.Exists(path))
                    {

                        colorTexture[i] = TextureLoader.FromFile(dev, path, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                    }
                }
            }
            return colorTexture;
        }
        /// <summary>
        /// Nacteni normalovych textur
        /// </summary>
        /// <param name="textureUrl">Pole s adresami k texturam</param>
        /// <returns>Pole normalovych textur</returns>
        private Texture[] loadNormalTextures(string[] textureUrl)
        {
            Texture[] normalTexture = new Texture[textureUrl.Length];
            string path;


            for (int i = 0; i < textureUrl.Length; i++)
            {
                bool createtexture = true;
                for (int t = 0; t < i; t++)
                {
                    if (textureUrl[t] == textureUrl[i])
                    {
                        normalTexture[i] = normalTexture[t];
                        createtexture = false;
                        break;
                    }
                }
                if (createtexture)
                {
                    if (textureUrl[i] != null)
                    {
                        path = textureUrl[i].Replace(textureUrl[i].Substring(textureUrl[i].Length - 4), "_normal.dds");

                        if (File.Exists(path))
                        {
                            normalTexture[i] = TextureLoader.FromFile(dev, path, 0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                        }
                    }
                }
            }
            return normalTexture;
        }
        /// <summary>
        /// Nacteni bitmapy ze souboru
        /// </summary>
        /// <param name="loaded">Entita do ktere bude pridan parametr s bitmapou</param>
        /// <param name="bitmapUrl">Adresa bitmapy</param>
        /// <param name="name">Jmeno bitmapy</param>
        /// <returns>Entita s bitmapou</returns>
        private Entity LoadBitMap(Entity loaded, string bitmapUrl, string name)
        {
            if (File.Exists(bitmapUrl))
            {

                Image bitMapa = Bitmap.FromFile(bitmapUrl);
                loaded.AddParametr(new Parametr("System.Drawing.Image", name, bitMapa));

            }
            return loaded;
        }
        /// <summary>
        /// Nacteni shaderu ze souboru
        /// </summary>
        /// <param name="loaded">Entita do ktere bude pridan parametr s shaderem</param>
        /// <param name="shaderUrl">Adresa k shaderu</param>
        /// <param name="name">jmeno shaderu</param>
        /// <returns>Entita s shaderem</returns>
        private Entity LoadShaderFromFile(Entity loaded, string shaderUrl, string name)
        {
            if (File.Exists(shaderUrl))
            {
                string er = String.Empty;
                Effect ef = Effect.FromFile(dev, shaderUrl, null, null, ShaderFlags.None, shaderPool, out er);
                if (er != String.Empty)
                    throw new Exception("Chyba pri nacitani shaderu " + shaderUrl + " " + er);

                loaded.AddParametr(new Parametr("Microsoft.DirectX.Direct3D.Effect", name, ef));

            }
            return loaded;
        }

        /// <summary>
        /// Nacteni animovaneho modelu
        /// </summary>
        /// <param name="loaded">>Entita do ktere bude pridan parametr s animaci</param>
        /// <param name="objectUrl">Adresa k souboru .X s animaci</param>
        /// <param name="name">Jmeno modelu</param>
        /// <returns>Entitu s animaci</returns>
        private Entity LoadAnimatedObject(Entity loaded, string objectUrl, string name)
        {
            
            AllocateHierarchy alloc;
            alloc = new WiccanRede.Graphics.Scene.SpecialObjects.AnimatedObject.AnimationAllocation();

            try
            {
                rootFrame = Mesh.LoadHierarchyFromFile(objectUrl, MeshFlags.Managed, dev, alloc, null);
            }
            catch (Exception)
            {
                Console.WriteLine("Chyba pri nacitani ze souboru");
            }

            SetupBoneMatrices(rootFrame.FrameHierarchy as anim.AnimationFrame);

            List<MeshContainer> meshes = new List<MeshContainer>();
            Frame rf = rootFrame.FrameHierarchy;
            getAnimationMesh(rf, meshes);

            MeshContainer mc = meshes[0];
            ExtendedMaterial[] materials = mc.GetMaterials();
            Texture[] textures = new Texture[materials.Length];
            string pathname = Path.GetDirectoryName(objectUrl) + @"\";

            // load textures 
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].TextureFilename != null)
                {

                    if (File.Exists((pathname + materials[i].TextureFilename)))
                    {
                        textures[i] = TextureLoader.FromFile(dev, (pathname + materials[i].TextureFilename),
                            0, 0, 0, Usage.None, Format.R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
                    }

                }

            }

            loaded.AddParametr(new Parametr("Objekt[]", "textures", textures));
            loaded.AddParametr(new Parametr("Microsoft.DirectX.Direct3D.AnimationRootFrame", name, rootFrame));
            loaded.AddParametr(new Parametr("Microsoft.DirectX.Direct3D.ProgressiveMesh", "pMesh", GetMesh(rootFrame)));
           return loaded;
        }
        /// <summary>
        /// Nalezeni framu s meshem
        /// </summary>
        /// <param name="rootFrame">rootframe animace</param>
        /// <returns>frame s meshem</returns>
        private void getAnimationMesh(Frame frame, List<MeshContainer> meshes)
        {
            if (frame.MeshContainer != null)
                meshes.Add(frame.MeshContainer);
            if(frame.FrameFirstChild != null)
                getAnimationMesh(frame.FrameFirstChild, meshes);
            if(frame.FrameSibling != null)
                getAnimationMesh(frame.FrameSibling, meshes);
        }
        /*private MeshContainer getAnimationMesh(AnimationRootFrame rootFrame,List<MeshContainer> meshes)
        {
            
            Frame rf = rootFrame.FrameHierarchy;
            while (rf.MeshContainer == null)
            {
                if(rf.FrameFirstChild == null)
                {
                    rf =rf.FrameSibling;
                }
                else{
                rf = rf.FrameFirstChild;
                }
            }
            return (rf.MeshContainer);
        }*/

        /// <summary>
        /// Nastaveni kosti animace
        /// </summary>
        /// <param name="frame">RootFrame animace</param>
        private void SetupBoneMatrices(anim.AnimationFrame frame)
        {
            if (frame == null)
                return;

            // First do the mesh container this frame contains (if it does)
            if (frame.MeshContainer != null)
            {
                SetupBoneMatrices(frame.MeshContainer as anim.AnimationMeshContainer);
            }
            // Next do any siblings this frame may contain
            if (frame.FrameSibling != null)
            {
                SetupBoneMatrices(frame.FrameSibling as anim.AnimationFrame);
            }
            // Finally do the children of this frame
            if (frame.FrameFirstChild != null)
            {
                SetupBoneMatrices(frame.FrameFirstChild as anim.AnimationFrame);
            }
        }
        /// <summary>
        /// nastaveni kosti animace
        /// </summary>
        /// <param name="mesh">frame s animaci</param>
        private void SetupBoneMatrices(anim.AnimationMeshContainer mesh)
        {
            // Is there skin information?  If so, setup the matrices
            if (mesh.SkinInformation != null)
            {
                int numberBones = mesh.SkinInformation.NumberBones;

                anim.AnimationFrame[] frameMatrices = new anim.AnimationFrame[numberBones];
                for (int i = 0; i < numberBones; i++)
                {
                    anim.AnimationFrame frame = Frame.Find(rootFrame.FrameHierarchy,
                        mesh.SkinInformation.GetBoneName(i)) as anim.AnimationFrame;

                    if (frame == null)
                        throw new InvalidOperationException("Could not find valid bone.");

                    frameMatrices[i] = frame;
                }
                mesh.SetFrames(frameMatrices);
            }
        }
        /// <summary>
        /// Vytvoreni progressive meshe
        /// </summary>
        /// <param name="rootFrame">rootframe animace</param>
        /// <returns>vztvoreni progressive mesh</returns>
        private ProgressiveMesh GetMesh(AnimationRootFrame rootFrame)
        {
            ProgressiveMesh pm = null;
            Frame rf = rootFrame.FrameHierarchy;
            List<MeshContainer> meshes = new List<MeshContainer>();
            getAnimationMesh(rf, meshes);
            anim.AnimationMeshContainer container = meshes[0] as anim.AnimationMeshContainer;

            int use32Bit = (int)(container.MeshData.Mesh.Options.Value & MeshFlags.Use32Bit);
            GraphicsStream adjacency = container.GetAdjacencyStream();//container.adjency;

            using (Mesh currentmesh = Mesh.Clean(CleanType.Simplification, container.MeshData.Mesh, adjacency, adjacency))
            {

                WeldEpsilons epsilons = new WeldEpsilons();
                currentmesh.WeldVertices(0, epsilons, adjacency, adjacency);
                currentmesh.Validate(adjacency);

                Mesh newmesh = currentmesh.Optimize(MeshFlags.OptimizeStripeReorder | MeshFlags.OptimizeAttributeSort, adjacency);
                using (newmesh = currentmesh.Clone(MeshFlags.Managed | (MeshFlags)use32Bit, GeneralObject.GeneralVertex.vertexElements, container.MeshData.Mesh.Device))
                {
                    newmesh.ComputeNormals();

                    pm = new ProgressiveMesh(newmesh, adjacency, null, 1, MeshFlags.SimplifyFace);
                   
                }

            }

            return (pm);
        }
    
    }


}
