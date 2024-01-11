CREATE FUNCTION [dbo].[_nb_GetActivity_Function]
()
    RETURNS TABLE
        AS

        RETURN (SELECT
                    RTRIM(au.username) [USERID],
                    RTRIM(su.Display_Name) [USERNAME],
                    RTRIM(comp.CMPNYNAM) [CMPNYNAM],
                    au.logintime [LOGIN_DATE_TIME],
                    au.lastpingtime [last_batch],
                    DATEDIFF( MINUTE, au.logintime, GETDATE( ) ) [Session_Length],
                    DATEDIFF( MINUTE, au.lastpingtime, GETDATE( ) ) [Idle_Time],
                    RTRIM(au.machinename) [hostname],
                    'SalesPad ' + au.appversion [program_name],
                    COALESCE (lc.Lock_Count, 0) [Lock_Count],
                    COALESCE (bcnt.Batch_Count, 0) [Batch_Count]
                FROM
                    DYNAMICS..spActiveUsers au
                        LEFT JOIN spSystemUser su ON au.username = su.User_Name
                        LEFT JOIN (SELECT User_Name, COUNT(*) [Lock_Count] FROM spvActivityLock GROUP BY User_Name) lc ON au.username = lc.User_Name
                        LEFT JOIN DYNAMICS..SY01500 comp ON comp.INTERID = au.company
                        LEFT JOIN (SELECT USERID, COUNT(*) [Batch_Count] FROM SY00500 GROUP BY USERID) bcnt ON bcnt.USERID = au.username

                UNION

                SELECT
                    RTRIM(a.USERID) [USERID],
                    RTRIM(u.USERNAME) [USERNAME],
                    RTRIM(a.CMPNYNAM) [CMPNYNAM],
                    a.LOGINDAT + a.LOGINTIM LOGIN_DATE_TIME,
                    sp.last_batch,
                    DATEDIFF( MINUTE, a.LOGINDAT + a.LOGINTIM, GETDATE( ) ) [Session_Length],
                    COALESCE(DATEDIFF( MINUTE, last_batch, GETDATE( ) ), 0) [Idle_Time],
                    COALESCE(RTRIM(hn.hostname), RTRIM(sp.hostname)) [hostname],
                    CASE
                        sp.program_name
                        WHEN '' THEN
                            aver.ProgramName ELSE sp.program_name
                        END AS [program_name],
                    COALESCE ( cnt.Lock_Count, 0 ) [Lock_Count],
                    COALESCE (bcnt.Batch_Count, 0) [Batch_Count]
                FROM
                    DYNAMICS..ACTIVITY a
                        LEFT JOIN tempdb..DEX_SESSION ds ON a.SQLSESID = ds.session_id
                        LEFT JOIN master..sysprocesses sp ON ds.sqlsvr_spid = sp.spid
                        LEFT JOIN ( SELECT USERID, COUNT ( * ) [Lock_Count] FROM DYNAMICS..SY00800 GROUP BY USERID ) cnt ON a.USERID = cnt.USERID
                        LEFT JOIN DYNAMICS..SY01400 u ON a.USERID = u.USERID
                        LEFT JOIN DYNAMICS..SY01500 comp ON comp.CMPNYNAM = a.CMPNYNAM
                        LEFT JOIN (SELECT db_name, 'Microsoft Dynamics GP ' + CAST(db_verMajor as VARCHAR(MAX)) + '.' + CAST(db_verMinor as VARCHAR(MAX)) + '.' + CAST(db_verBuild as VARCHAR(MAX)) [ProgramName] FROM DYNAMICS..DB_Upgrade WHERE PRODID = 0) aver ON aver.db_name = comp.INTERID
                        CROSS APPLY (SELECT TOP 1 loginame, hostname FROM master..sysprocesses WHERE loginame = a.USERID AND hostname <> '') AS hn
                        LEFT JOIN (SELECT USERID, COUNT(*) [Batch_Count] FROM SY00500 GROUP BY USERID) bcnt ON bcnt.USERID = a.USERID )
go