using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CsvHelper;

public class ExcelImporter : MonoBehaviour
{
    // Resources
    private const string ROOT            = "Assets/Data";
    private const string CSV_PROMPT_NAME = "PromptTable";
    // Assets
    private const string PROMPT_PATH     = "Generated";
    
    [MenuItem("Tools/CSV → Prompt")]
    public static void ConvertCsvAndGenerateDatabase()
    {
        // --- Pass 1: 모든 에셋을 빈 껍데기로 먼저 생성하고, 필요한 정보를 수집합니다. ---
        
        // 모든 출력 폴더를 먼저 확인하고 생성합니다.
        EnsureFolderExists($"{ROOT}/{PROMPT_PATH}");
        
        // Prompt SO 껍데기 생성
        LoadOrCreateAsset<PromptSO>($"{ROOT}/{PROMPT_PATH}/Prompt.asset");
        
        // 모든 변경사항 동기화
        AssetDatabase.SaveAssets(); 
        AssetDatabase.Refresh();
        
        // --- Pass 2: 이제 모든 에셋이 확실히 존재하므로, 데이터를 채웁니다. ---
        
        // Prompt SO 로드 후 데이터 채우기
        PromptSO prompt = AssetDatabase.LoadAssetAtPath<PromptSO>($"{ROOT}/{PROMPT_PATH}/Prompt.asset");
        PopulateFromCsv(prompt, $"{ROOT}/{CSV_PROMPT_NAME}");
        
        // --- 최종 저장 ---
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Success: CSV data has been converted to ScriptableObjects and Database!");
    }
    
    /// <summary>
    /// 지정된 경로의 CSV 파일을 읽어 Prompt SO를 채웁니다.
    /// </summary>
    private static void PopulateFromCsv(PromptSO prompt, string csvResourcePath)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(csvResourcePath);
        if (csvFile == null)
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: Resources/{csvResourcePath}.csv");
            return;
        }
        
        prompt.ResetData();

        // List를 직접 채웁니다. Dictionary를 직접 제어하지 않습니다.
        using (var reader = new StringReader(csvFile.text))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                if (csv.Context.Parser is not { Record: { Length: < 20 } }) continue;
                var cols = csv.Context.Parser.Record;
                
                prompt.AddPrompt(int.Parse(cols[0]), cols[1]);
            }
        }
        EditorUtility.SetDirty(prompt); // Prompt 에셋이 변경되었음을 알립니다.
    }
    
    /// <summary>
    /// 지정된 경로의 에셋을 로드하고, 없으면 새로 생성하여 반환하는 제네릭 헬퍼 함수입니다.
    /// </summary>
    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
    
    /// <summary>
    /// 폴더 존재 여부를 확인하고 없으면 생성하는 헬퍼 함수입니다.
    /// </summary>
    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}