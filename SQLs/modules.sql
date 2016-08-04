SELECT 
	m.*, GROUP_CONCAT(f.Name SEPARATOR ', ') AS Functions
FROM Modules m
	LEFT JOIN ModuleFunctions mf ON mf.ModuleID = m.ID
    LEFT JOIN Functions f ON f.ID = mf.FunctionID
GROUP BY m.ID;