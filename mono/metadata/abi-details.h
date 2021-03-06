/*
 * Copyright 2014 Xamarin Inc
 */
#ifndef __MONO_METADATA_ABI_DETAILS_H__
#define __MONO_METADATA_ABI_DETAILS_H__

#include <config.h>
#include <glib.h>

#define MONO_ABI_ALIGNOF(type) MONO_ALIGN_ ## type
#define MONO_CURRENT_ABI_ALIGNOF(type) ((int)G_STRUCT_OFFSET(struct { char c; type x; }, x))
#define MONO_ABI_SIZEOF(type) MONO_SIZEOF_ ## type
#define MONO_CURRENT_ABI_SIZEOF(type) ((int)sizeof(type))

#undef DECL_OFFSET
#undef DECL_OFFSET2
#define DECL_OFFSET(struct,field) MONO_OFFSET_ ## struct ## _ ## field = -1,
#define DECL_OFFSET2(struct,field,offset) MONO_OFFSET_ ## struct ## _ ## field = offset,
#define DECL_ALIGN(type) MONO_ALIGN_ ##type = MONO_CURRENT_ABI_ALIGNOF (type),
#define DECL_ALIGN2(type,size) MONO_ALIGN_ ##type = size,
#define DECL_SIZE(type) MONO_SIZEOF_ ##type = MONO_CURRENT_ABI_SIZEOF (type),
#define DECL_SIZE2(type,size) MONO_SIZEOF_ ##type = size,

enum {
#include "object-offsets.h"
};

#ifdef USED_CROSS_COMPILER_OFFSETS
#define MONO_STRUCT_OFFSET(struct,field) MONO_OFFSET_ ## struct ## _ ## field
#else
#if defined(HAS_CROSS_COMPILER_OFFSETS) || defined(MONO_CROSS_COMPILE)
#define MONO_STRUCT_OFFSET(struct,field) (MONO_OFFSET_ ## struct ## _ ## field == -1, G_STRUCT_OFFSET (struct,field))
#else
#define MONO_STRUCT_OFFSET(struct,field) G_STRUCT_OFFSET (struct,field)
#endif
#endif

#endif
