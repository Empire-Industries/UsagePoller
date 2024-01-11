create table activityTrackingLog
(
    userID          varchar(255),
    companyName     varchar(255),
    hostname        varchar(255),
    applicationName varchar(255),
    loginTime       datetime2,
    sessionLength   int,
    idleLength      int,
    updateTime      datetime2,
    lockCount       int,
    batchCount      int,
    sessionHash     varchar(512)
)
go

grant delete, insert, select, update on activityTrackingLog to DYNGRP
go