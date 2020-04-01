using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede.Graphics.Scene
{

    /// <summary>
    /// Vycet urcujici typ svetla
    /// </summary>
    public enum LightType
    {
        /// <summary>
        /// Smerove svetlo
        /// </summary>
        Direction,
        /// <summary>
        /// Vsesmerove svetlo
        /// </summary>
        Point
    };

    /// <summary>
    /// Struktura s plochami frustrumu
    /// </summary>
    public struct ClipVolume
    {
        /// <summary>
        /// Leva a prava orezova plocha frustrumu
        /// </summary>
        public Plane pLeft, pRight;
        /// <summary>
        /// Horni a dolni orezova plocha frustrumu
        /// </summary>
        public Plane pTop, pBottom;
        /// <summary>
        /// Blizka a vzdalena orezova plocha frustrumu
        /// </summary>
        public Plane pNear, pFar;
    }

    public interface IRenderable
    {
        /// <summary>
        /// Nastavi viditelnost objektu
        /// </summary>
        /// <param name="visible">Pri nastaveni false se objekt nikdy nenakresli, pri true, pokud je videt ano</param>
        void SetVisible(bool visible);

        /// <summary>
        /// Funkce nastavi world matici a spocita a nastavi i inverzni transponovanou world matici
        /// </summary>
        /// <param name="worldMatrix">World matice, ktera se ma nastavit</param>
        void SetMatrixWorld(Matrix worldMatrix);

        /// <summary>
        /// Vrati pocet vertexu, ktere se renderuji
        /// </summary>
        /// <returns>Vrati pocet vertexu, ktere se renderuji</returns>
        int GetVertexCount();

        /// <summary>
        /// Vrati pocet trojuhelniku, ktere se renderuji
        /// </summary>
        /// <returns>Vrati pocet trojuhelniku, ktere se renderuji</returns>
        int GetFacesCount();

        /// <summary>
        /// Funkce vraci pocet subsetu v meshi
        /// </summary>
        /// <returns>Funkce vraci pocet subsetu v meshi</returns>
        int GetSubsetCount();

        /// <summary>
        /// Vrati, zda je objekt viditelny, ci ne z pohledu kamery
        /// </summary>
        /// <returns>Vrati, zda je objekt viditelny, ci ne z pohledu kamery</returns>
        bool GetComputedVisibility();

        /// <summary>
        /// Vrati vzdalenost od pozice
        /// </summary>
        /// <returns>Vrati vzdalenost od pozice</returns>
        /// <remarks>Jedna se o vzdalenost od pozice, ktera byla naposled predana funkci ComputeDistanceToPosition()</remarks>
        /// <seealso cref="ComputeDistanceToPosition"/>
        float GetDistanceToPosition();

        /// <summary>
        /// Vrati vsechny world matice
        /// </summary>
        /// <returns></returns>
        Matrix[] GetMatricesWorld();

        /// <summary>
        /// Vrati inverzni transponovane world matice
        /// </summary>
        /// <returns>Vrati inverzni transponovane world matice</returns>
        Matrix[] GetMatricesWorldIT();

        /// <summary>
        /// Vrati world matici
        /// </summary>
        /// <returns>Vraci world matici</returns>
        Matrix GetMatrixWorld();

        /// <summary>
        /// Vraci inverzni transponovanou world matici
        /// </summary>
        /// <returns>Vraci inverzni transponovanou world matici</returns>
        Matrix GetMatrixWorldIT();

        /// <summary>
        /// Metoda vrati world matici bounding meshe
        /// </summary>
        /// <returns>Metoda vrati world matici bounding meshe</returns>
        Matrix GetMatrixWorldBoundingSphereMesh();

        /// <summary>
        /// Metoda vrati vsechny inverzni transponovany world matice bounding boxu
        /// </summary>
        /// <returns>Metoda vrati vsechny inverzni transponovany world matice bounding boxu</returns>
        Matrix[] GetMatricesWorldITBoundingMesh();

        /// <summary>
        /// Metoda vrati vsechny world matice bounding boxu
        /// </summary>
        /// <returns>Metoda vrati vsechny world matice bounding boxu</returns>
        Matrix[] GetMatricesWorldBoundingMesh();

        /// <summary>
        /// Metoda vrati world matici inverzni a transponovanou bounding meshe
        /// </summary>
        /// <returns>Metoda vrati world matici inverzni a transponovanou bounding meshe</returns>
        Matrix GetMatrixWorldITBoundingSphereMesh();

        /// <summary>
        /// Metoda vrati pocet volnosti
        /// </summary>
        /// <returns>Vrati pocet volnosti</returns>
        int GetMatrixWorldCount();

        /// <summary>
        /// Spocita viditelnost objektu. Na zaklade vypocte se objekt bude nebo nebude renderovat
        /// </summary>
        bool ComputeVisibility(ISceneCamera camera);

        /// <summary>
        /// Spocte vzdalenost objektu od zadane pozice
        /// </summary>
        /// <param name="position">Pozice, od ktere se pocita vzdalenost</param>
        /// <returns>Vraci vzdalenost od zadane pozice</returns>
        float ComputeDistanceToPosition(Vector3 position);

        /// <summary>
        /// Metoda zrusi predchozi vypocty viditelnosti (objekt bude viden)
        /// </summary>
        void ResetVisibility();

        /// <summary>
        /// Funkce slouzi pro rendering vlastniho objektu
        /// </summary>
        /// <param name="subset">Subset, ktery se ma kreslit</param>
        void Render(int subset);

        /// <summary>
        /// Funkce slouzi pro rendering bounding boxu objektu
        /// </summary>
        void RenderBoundingSphereMesh();
    }

    /// <summary>
    /// Interface definujici zakladni objekt urceny pro rendering
    /// </summary>
    public interface IGeneralObject : IRenderable
    {
        /// <summary>
        /// Provede uklid textur, meshe a bounding boxu
        /// </summary>
        void Dispose();

        /// <summary>
        /// Metoda se vola pri ztrate zarizeni pro obnovu zdroju
        /// </summary>
        void ReInit();

        /// <summary>
        /// Metoda uvolni vsechny zdroje pri ztrate zarizeni
        /// </summary>
        void Releasse();

        /// <summary>
        /// Funkce se automaticky spousti pri prihozeni objektu na rendering, slouzi pro inicializaci nestandartnich hodnot do shaderu
        /// </summary>
        /// <param name="effect">Effect, pres ktery je objekt renderovan</param>
        void InitShaderValue(Effect effect);

        /// <summary>
        /// Funkce je volana automaticky vzdy pred funkci UpdateShaderValue, slouzi k aktualizaci hodnot zavislych na case
        /// </summary>
        /// <param name="time">Cas v milisekundach od spusteni aplikace</param>
        void Update(float time);

        /// <summary>
        /// Metoda se automaticky spousti pred renderingem kazdeho subsetu
        /// </summary>
        /// <param name="subset">Subset, ktery se bude zrovna renderovat</param>
        void UpdateSubset(int subset);

        /// <summary>
        /// Funkce provede update hodnot v shaderu - je automaticky volana pred vykreslenim objektu
        /// </summary>
        /// <param name="effect">Effect, pres ktery je objekt renderovan</param>
        void UpdateShaderValue(Effect effect);

        /// <summary>
        /// Nastavi model, ktery bude pouzit pro rendering
        /// </summary>
        /// <param name="mesh">Model</param>
        void SetModel(BaseMesh mesh);

        /// <summary>
        /// Nastavi bounding box model
        /// </summary>
        /// <param name="mesh">Bounding box model</param>
        void SetBoundingBoxModel(Mesh mesh);

        /// <summary>
        /// Metoda nastavi kvalitu modelu, ktery se bude renderovat
        /// </summary>
        /// <remarks>Nastavuje kvalitu jak z pohledu geometrie, tak textur, efektu, atp., zavisi na konkretnim objektu</remarks>
        /// <param name="quality">Uroven kvality, plati na intervalu (0,1) vcetne krajnich bodu, kde 0 je nejnizsi kvalita a 1 je nejvyssi kvalita</param>
        void SetObjectQuality(float quality);

        /// <summary>
        /// Povoli/zakaze LOD objektu
        /// </summary>
        void EnableLOD(bool enableLOD);

        /// <summary>
        /// Nastavi novou pozici objektu
        /// </summary>
        /// <param name="position">Pozice objektu ve world space</param>
        void SetPosition(Vector3 position);

        /// <summary>
        /// Natoci objekt pozadovanym smerem
        /// </summary>
        /// <param name="direction">Smer, do ktereho se ma objekt natocit</param>
        void SetDirection(Vector3 direction);

        /// <summary>
        /// Metoda slouzi pro nastaveni priznaku, ze objekt se nachazi vsude
        /// </summary>
        /// <param name="isEveryWhere">True, pokud se objekt nachazi vsude, jinak false</param>
        void SetIsEveryWhere(bool isEveryWhere);

        /// <summary>
        /// Vraci model, ktery se bude prave renderovat
        /// </summary>
        /// <returns>Vraci model, ktery se bude prave renderovat</returns>
        BaseMesh GetModel();

        /// <summary>
        /// Vrati mesh s bounding boxem
        /// </summary>
        /// <returns>Vrati mesh s bounding boxem</returns>
        Mesh GetBoundingModel();

        /// <summary>
        /// Vraci souradnice stredu objektu
        /// </summary>
        /// <returns>Bod se souradnicema objektu</returns>
        Vector3 GetSphereObjectCenter();

        /// <summary>
        /// Vraci souradnice stredu objektu ve World souradnicich
        /// </summary>
        /// <returns>Metoda vrati pozici stredu bounding boxu</returns>
        Vector3 GetBoundingBoxCenter();

        /// <summary>
        /// Vraci souradnice stredu objektu
        /// </summary>
        /// <returns>Bod se souradnicema objektu</returns>
        Vector3 GetSphereObjectRelativeCenter();

        /// <summary>
        /// Metoda vrati pozici stredu bounding boxu
        /// </summary>
        /// <returns>Metoda vrati pozici stredu bounding boxu</returns>
        Vector3 GetBoundingBoxRelativeCenter();

        /// <summary>
        /// Vrati souradnice bodu na nejnizsich souradnicich
        /// </summary>
        /// <returns>Vrati souradnice bodu na nejnizsich souradnicich</returns>
        Vector3 GetBoundingBoxRelativeMinimum();

        /// <summary>
        /// Vrati souradnice bodu na nejvyssich souradnicich
        /// </summary>
        /// <returns>Vrati souradnice bodu na nejvyssich souradnicich</returns>
        Vector3 GetBoundingBoxRelativeMaximum();

        /// <summary>
        /// Metoda vrati normaly kazde steny bounding boxu (ve world souradnicich)
        /// </summary>
        /// <returns>Metoda vrati normalizovane normaly kazde steny bounding boxu (ve world souradnicich)</returns>
        /// <remarks>Metoda je zavisla na predchozim volani metody CreateBoundingBoxNormals (je automaticky volana z konstruktoru teto tridy)</remarks>
        /// <seealso cref="CreateBoundingBoxNormals"/>
        Vector3[] GetBoxObjectNormals();

        /// <summary>
        /// Vrati puvodni World matici, ktera byla objektu nastavena
        /// </summary>
        /// <returns>Vrati puvodni World matici, ktera byla objektu nastavena</returns>
        Matrix GetMatrixWorldOriginal();

        /// <summary>
        /// Vrati pozici objektu
        /// </summary>
        /// <returns>Vrati pozici objektu</returns>
        Vector3 GetPosition();

        /// <summary>
        /// Vraci radius od stredu obalove koule
        /// </summary>
        /// <returns>Radius obalove koule</returns>
        float GetSphereRadius();

        /// <summary>
        /// Metoda vrati vzdalenost od kamery
        /// </summary>
        /// <remarks>Vzdalenost je nejdrive nutne vypocitat pomoci metody ComputeDistanceFromCamera</remarks>
        /// <returns>Vzdalenost od kamery</returns>
        /// <seealso cref="ComputeDistanceFromCamera"/>
        float GetDistanceFromCamera();

        /// <summary>
        /// Metoda spocte vzdalenost od kamery a pokud je zapnute LOD, tak ho nastavi
        /// </summary>
        /// <returns>Vzdalenost od kamery</returns>
        float ComputeDistanceFromCamera(ISceneCamera camera);

        /// <summary>
        /// Vrati, zda objekt pouziva alpha blending
        /// </summary>
        /// <returns>Vrati, zda objekt pouziva alpha blending</returns>
        bool GetUseAlphaBlending();

        /// <summary>
        /// Metoda slouzi pro zjisteni priznaku, zda objekt se nachazi vsude
        /// </summary>
        /// <param name="isEveryWhere">True, pokud se objekt nachazi vsude, jinak false</param>
        bool GetIsEveryWhere();

        /// <summary>
        /// Metoda vrati kvalitu modelu, ktery se bude renderovat
        /// </summary>
        /// <returns>Uroven kvality, plati na intervalu (0,1) vcetne krajnich bodu, kde 0 je nejnizsi kvalita a 1 je nejvyssi kvalita</returns>
        float GetObjectQuality();

        /// <summary>
        /// Vrati pole textur nultyho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur nultyho bufferu s klasickyma texturama</returns>
        Texture[] GetTexturesColor0();

        /// <summary>
        /// Vrati pole textur prvniho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur prvniho bufferu s klasickyma texturama</returns>
        Texture[] GetTexturesColor1();

        /// <summary>
        /// Vrati pole textur druhyho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur druhyho bufferu s klasickyma texturama</returns>
        Texture[] GetTexturesColor2();

        /// <summary>
        /// Vrati pole textur bufferu s normalovyma texturama
        /// </summary>
        /// <returns>Vrati pole textur bufferu s normalovyma texturama</returns>
        Texture[] GetTexturesNormal();

        /// <summary>
        /// Zjisti, zda je zaply LOD, ci ne
        /// </summary>
        /// <returns>Vrati true, pokud se LOD uplatnuje, jinak false</returns>
        bool isEnableLOD();

        /// <summary>
        /// Vrati, zda objekt, potrebuje pro vyrenderovani alpha blendovani, ci ne
        /// </summary>
        /// <returns>Vrati, zda objekt, potrebuje pro vyrenderovani alpha blendovani, ci ne</returns>
        bool isAlphaObject();

        /// <summary>
        /// Vrati, zda je objekt uklizen, ci ne
        /// </summary>
        /// <returns>Vrati true, pokud je jiz objekt uklizen, jinak false</returns>
        bool isDisposed();

        /// <summary>
        /// Nastavi objektu, zda je nekym drzen, ci ne
        /// </summary>
        void SetEquiped(bool equiped);

        /// <summary>
        /// Zjisti, zda je objekt nekym drzen, ci ne
        /// </summary>
        /// <returns>Vraci true, pokud objekt nekdo drzi</returns>
        bool isEquiped();

        /// <summary>
        /// Metoda zjisti kolizi bodu s objektem
        /// </summary>
        /// <param name="position">Pozice bodu, kde se ma nachazet kolize</param>
        /// <param name="camera"></param>
        /// <returns>Vraci, zda nastala kolize, ci ne</returns>
        bool ComputeSphereCollission(Vector3 position);

        /// <summary>
        /// Metoda zjisti kolizi bodu s objektem
        /// </summary>
        /// <param name="position">Pozice bodu, kde se ma nachazet kolize</param>
        /// <returns>Vraci, zda nastala kolize, ci ne</returns>
        bool ComputeBoxCollission(Vector3 position);

        /// <summary>
        /// Metoda provede aplikaci LODu a prepocita vertexy a facy
        /// </summary>
        void ApplyLOD();

        /// <summary>
        /// Metoda vrati world matici bounding boxu meshe
        /// </summary>
        /// <returns>Metoda vrati world matici bounding boxu meshe</returns>
        Matrix GetMatrixWorldBoundingBoxMesh();

        /// <summary>
        /// Metoda vrati world matici inverzni a transponovanou bounding boxu meshe
        /// </summary>
        /// <returns>Metoda vrati world matici inverzni a transponovanou bounding sphery meshe</returns>
        Matrix GetMatrixWorldITBoundingBoxMesh();
    }

    /// <summary>
    /// Interface definujici zakladni metody svetla
    /// </summary>
    public interface ISceneLight
    {
        /// <summary>
        /// Funkce vrati pozici svetla
        /// </summary>
        /// <returns>Vraci pozici svetla (predpoklada se, ze posledni slozka je 1 a pozicice je v Euklidovskym prostoru)</returns>
        Vector4 GetLightPosition();
        
        /// <summary>
        /// Vrati intenzitu svetla
        /// </summary>
        /// <returns>Vrati intenzitu svetla</returns>
        float GetAttuentation();

        /// <summary>
        /// Vrati typ svetla
        /// </summary>
        /// <returns>Typ svetla</returns>
        LightType GetType();

        /// <summary>
        /// Nastavi intenzitu svetla
        /// </summary>
        /// <param name="att">Intenzita, ktera se ma svetlu nastavit</param>
        void SetAttuentation(float att);

        /// <summary>
        /// Nastavi typ svetla
        /// </summary>
        /// <param name="type">Typ svetla, ktery se ma svetlu priradit</param>
        void SetType(LightType type);

        /// <summary>
        /// Zjisti, zda je svetlo vyple nebo zaple
        /// </summary>
        /// <returns>Vraci true, pokud svetlo sviti, jinak false</returns>
        bool isEnable();
    }

    /// <summary>
    /// Interface definujici zakladni metody kamery
    /// </summary>
    public interface ISceneCamera
    {
        /// <summary>
        /// Vrati pohledovou matici kamery
        /// </summary>
        /// <returns>View matice</returns>
        Matrix GetMatrixView();

        /// <summary>
        /// Vrati projekcni matici kamery
        /// </summary>
        /// <returns>Projection matice</returns>
        Matrix GetMatrixProjection();

        /// <summary>
        /// Vrati nasobek pohledove a projekcni matice
        /// </summary>
        /// <returns>view * projection matice</returns>
        Matrix GetMatrixViewProjection();

        /// <summary>
        /// Vrati orezove plochy frustrumu
        /// </summary>
        /// <returns>Orezove plochy frustrumu</returns>
        ClipVolume GetClipVolume();

        /// <summary>
        /// Metoda vrati pozici kamery
        /// </summary>
        Vector3 GetVector3Position();

        /// <summary>
        /// Metoda vrati bod, do ktereho se kamera kouka
        /// </summary>
        Vector3 GetVector3LookAt();

    }

}
