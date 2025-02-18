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
    typedef void (*OnBehaviourInsight)(const char *behaviourInsight);

    void EnableLogging(bool enable);
    void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight);
    void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    void NeftaPlugin_OnExternalAdShownAsString(const char *ss);
    const char * NeftaPlugin_GetNuid(void *instance, bool present);
    void NeftaPlugin_GetBehaviourInsight(const char *insights);
    void NeftaPlugin_SetOverride(const char *root);
#ifdef __cplusplus
}
#endif

NeftaPlugin *_plugin;

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId]];
    _plugin.OnBehaviourInsightAsString = ^void(NSString * _Nonnull behaviourInsight) {
        const char *cBI = [behaviourInsight UTF8String];
        onBehaviourInsight(cBI);
    };
}

void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    NSString *n = name ? [NSString stringWithUTF8String: name] : nil;
    NSString *cp = customPayload ? [NSString stringWithUTF8String: customPayload] : nil;
    [_plugin RecordWithType: type category: category subCategory: subCategory name: n value: value customPayload: cp];
}

void NeftaPlugin_OnExternalAdShownAsString(const char *ss) {
    NSString *s = [NSString stringWithUTF8String: ss];
    [NeftaPlugin OnExternalAdShownAsString: @"max" data: s];
}

const char * NeftaPlugin_GetNuid(void *instance, bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}

void NeftaPlugin_GetBehaviourInsight(const char *insights) {
    [_plugin GetBehaviourInsightWithString: [NSString stringWithUTF8String: insights]];
}

void NeftaPlugin_SetOverride(const char *root) {
    [_plugin SetOverrideWithUrl: [NSString stringWithUTF8String: root]];
}