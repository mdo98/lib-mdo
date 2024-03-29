/* multifit/gsl_multifit.h
 * 
 * Copyright (C) 2000, 2007, 2010 Brian Gough
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or (at
 * your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */

#ifndef __GSL_MULTIFIT_H__
#define __GSL_MULTIFIT_H__

#if !defined( GSL_FUN )
#  if !defined( GSL_DLL )
#    define GSL_FUN extern
#  elif defined( BUILD_GSL_DLL )
#    define GSL_FUN extern __declspec(dllexport)
#  else
#    define GSL_FUN extern __declspec(dllimport)
#  endif
#endif

#include <stdlib.h>
#include <gsl/gsl_math.h>
#include <gsl/gsl_vector.h>
#include <gsl/gsl_matrix.h>

#undef __BEGIN_DECLS
#undef __END_DECLS
#ifdef __cplusplus
# define __BEGIN_DECLS extern "C" {
# define __END_DECLS }
#else
# define __BEGIN_DECLS /* empty */
# define __END_DECLS /* empty */
#endif

__BEGIN_DECLS

typedef struct 
{
  size_t n; /* number of observations */
  size_t p; /* number of parameters */
  gsl_matrix * A;
  gsl_matrix * Q;
  gsl_matrix * QSI;
  gsl_vector * S;
  gsl_vector * t;
  gsl_vector * xt;
  gsl_vector * D;
} 
gsl_multifit_linear_workspace;

GSL_FUN gsl_multifit_linear_workspace *
gsl_multifit_linear_alloc (size_t n, size_t p);

GSL_FUN void
gsl_multifit_linear_free (gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_linear (const gsl_matrix * X,
                     const gsl_vector * y,
                     gsl_vector * c,
                     gsl_matrix * cov,
                     double * chisq,
                     gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_linear_svd (const gsl_matrix * X,
                         const gsl_vector * y,
                         double tol,
                         size_t * rank,
                         gsl_vector * c,
                         gsl_matrix * cov,
                         double *chisq, 
                         gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_linear_usvd (const gsl_matrix * X,
                          const gsl_vector * y,
                          double tol,
                          size_t * rank,
                          gsl_vector * c,
                          gsl_matrix * cov,
                          double *chisq, 
                          gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_wlinear (const gsl_matrix * X,
                      const gsl_vector * w,
                      const gsl_vector * y,
                      gsl_vector * c,
                      gsl_matrix * cov,
                      double * chisq,
                      gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_wlinear_svd (const gsl_matrix * X,
                          const gsl_vector * w,
                          const gsl_vector * y,
                          double tol,
                          size_t * rank,
                          gsl_vector * c,
                          gsl_matrix * cov,
                          double *chisq, 
                          gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_wlinear_usvd (const gsl_matrix * X,
                           const gsl_vector * w,
                           const gsl_vector * y,
                           double tol,
                           size_t * rank,
                           gsl_vector * c,
                           gsl_matrix * cov,
                           double *chisq, 
                           gsl_multifit_linear_workspace * work);

GSL_FUN int
gsl_multifit_linear_est (const gsl_vector * x,
                         const gsl_vector * c,
                         const gsl_matrix * cov, double *y, double *y_err);

GSL_FUN int
gsl_multifit_linear_residuals (const gsl_matrix *X, const gsl_vector *y,
                               const gsl_vector *c, gsl_vector *r);

__END_DECLS

#endif /* __GSL_MULTIFIT_H__ */
