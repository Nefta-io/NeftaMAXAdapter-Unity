//
//  NeftaPlugin_iOS.m
//  UnityFramework
//
//  Created by Tomaz Treven on 18/11/2023.
//

#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <NeftaSDK/NeftaSDK-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif
    void * NeftaPlugin_Init(const char *appId);
    void NeftaPlugin_Record(void *instance, const char *event);
    void NeftaPlugin_SetCustomBatchSize(void *instance, int newBatchSize);
#ifdef __cplusplus
}
#endif

NeftaPlugin_iOS *_plugin;

void * NeftaPlugin_Init(const char *appId)
{
    _plugin = [NeftaPlugin_iOS InitWithAppId: [NSString stringWithUTF8String: appId]];
    return (__bridge_retained void *)_plugin;
}

void NeftaPlugin_SetCustomBatchSize(void *instance, int newBatchSize)
{
    [_plugin SetCustomBatchSize: newBatchSize];
}

void NeftaPlugin_Record(void *instance, const char *event)
{
    [_plugin RecordWithEvent: [NSString stringWithUTF8String: event]];
}